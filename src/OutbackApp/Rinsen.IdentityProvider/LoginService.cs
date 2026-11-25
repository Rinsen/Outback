using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OtpNet;
using Rinsen.IdentityProvider.Exceptions;
using Rinsen.IdentityProvider.LocalAccounts;
using Rinsen.Outback.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Rinsen.IdentityProvider;

public class LoginService : ILoginService
{
    private static readonly TimeSpan UsedTotpLogMaxAge = TimeSpan.FromMinutes(10);

    // SEC-022: a 2FA session is a bearer token for the login in progress, so it
    // must expire. Ten minutes is plenty for the flow (start + code entry) and
    // matches the UsedTotpLog retention window.
    private static readonly TimeSpan TwoFactorSessionMaxAge = TimeSpan.FromMinutes(10);

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly RandomStringGenerator _randomStringGenerator;
    private readonly IIdentityService _identityService;
    private readonly ILogger<LoginService> _log;
    private readonly ILocalAccountService _localAccountService;
    private readonly IIdentityAttributeStorage _identityAttributeStorage;
    private readonly IUsedTotpLogStorage _usedTotpLogStorage;
    private readonly ITwoFactorAuthenticationSessionStorage _twoFactorAuthenticationSessionStorage;
    private readonly IdentityOptions _options;
    private readonly PasswordHashGenerator _passwordHashGenerator;

    public LoginService(ILocalAccountService localAccountService,
        IIdentityService identityService,
        IIdentityAttributeStorage identityAttributeStorage,
        IUsedTotpLogStorage usedTotpLogStorage,
        ITwoFactorAuthenticationSessionStorage twoFactorAuthenticationSessionStorage,
        IHttpContextAccessor httpContextAccessor,
        RandomStringGenerator randomStringGenerator,
        ILogger<LoginService> log,
        IdentityOptions options,
        PasswordHashGenerator passwordHashGenerator)
    {
        _localAccountService = localAccountService;
        _identityService = identityService;
        _identityAttributeStorage = identityAttributeStorage;
        _usedTotpLogStorage = usedTotpLogStorage;
        _twoFactorAuthenticationSessionStorage = twoFactorAuthenticationSessionStorage;
        _httpContextAccessor = httpContextAccessor;
        _randomStringGenerator = randomStringGenerator;
        _log = log;
        _options = options;
        _passwordHashGenerator = passwordHashGenerator;
    }

    public async Task<LoginResult> LoginAsync(string email, string password, string host, bool rememberMe)
    {
        LocalAccount localAccount;
        try
        {
            localAccount = await _localAccountService.GetLocalAccountAsync(email, password);
        }
        catch (InvalidPasswordException e)
        {
            _log.LogWarning(e, "Login failed for email {email}", email);

            return LoginResult.Failure();
        }
        catch (AccountLockedOutException e)
        {
            // SEC-016: locked-out account. Return the same generic failure as an
            // invalid password so the lockout is not distinguishable.
            _log.LogWarning(e, "Login blocked: account {email} is locked out", email);

            return LoginResult.Failure();
        }
        catch (AccountNotFoundException e)
        {
            _log.LogWarning(e, "Login failed for email {email}", email);

            // SEC-015: no account exists, so the real KDF never ran. Perform a dummy
            // PBKDF2 with the same cost as a real verification so that unknown and
            // known accounts are not distinguishable by response time.
            RunDummyPasswordHash(password);

            return LoginResult.Failure();
        }

        if (localAccount is not null && localAccount.Deleted is not null)
        {
            _log.LogWarning("Login blocked for deleted local account of email {email}", email);

            return LoginResult.Failure();
        }

        if (localAccount is not null && localAccount.IsDisabled)
        {
            // SEC-017: a disabled account must not authenticate. This check runs
            // before the 2FA branch so a disabled account with TOTP enabled is
            // rejected here, and it returns the same generic failure as a wrong
            // password so the state is not distinguishable.
            _log.LogWarning("Login blocked for disabled local account of email {email}", email);

            return LoginResult.Failure();
        }

        if (localAccount is not null && (localAccount.TwoFactorAppNotificationEnabled is not null || localAccount.TwoFactorEmailEnabled is not null
            || localAccount.TwoFactorSmsEnabled is not null || localAccount.TwoFactorTotpEnabled is not null))
        {
            string keyCode = string.Empty;
            if (localAccount.TwoFactorEmailEnabled is object || localAccount.TwoFactorSmsEnabled is object)
            {
                keyCode = _randomStringGenerator.GetRandomString(10);
            }

            var sessionId = _randomStringGenerator.GetRandomString(50);
            var twoFactorAuthenticationSession = new TwoFactorAuthenticationSession
            {
                Created = DateTimeOffset.Now,
                IdentityId = localAccount.IdentityId,
                KeyCode = keyCode,
                SessionId = sessionId,
                Type = TwoFactorType.NotSelected
            };

            await _twoFactorAuthenticationSessionStorage.Create(twoFactorAuthenticationSession);

            return LoginResult.RequireTwoFactor(twoFactorAuthenticationSession.SessionId, localAccount.TwoFactorEmailEnabled is object, localAccount.TwoFactorSmsEnabled is object, localAccount.TwoFactorTotpEnabled is object, localAccount.TwoFactorAppNotificationEnabled is object);
        }

        if (localAccount is null)
        {
            return LoginResult.Failure();
        }

        return await PrivateLogin(host, rememberMe, localAccount);
    }

    public async Task StartTotpFlow(string authSessionId)
    {
        var twoFactorAuthenticationSession = await _twoFactorAuthenticationSessionStorage.Get(authSessionId);

        if (twoFactorAuthenticationSession is not null)
        {
            twoFactorAuthenticationSession.Type = TwoFactorType.Totp;

            await _twoFactorAuthenticationSessionStorage.Update(twoFactorAuthenticationSession);
        }
        else
        {
            throw new TwoFactorAuthSessionNotFoundException("Two factor auth session not found");
        }
    }

    public async Task<LoginResult> ConfirmTotpCode(string authSessionId, string keyCode, string host, bool rememberMe)
    {
        // SEC-022: the storage lookup filters out deleted sessions (AND Deleted IS
        // NULL) and throws TwoFactorAuthSessionNotFoundException when none is
        // found, so a deleted session can never be completed.
        var twoFactorAuthenticationSession = await _twoFactorAuthenticationSessionStorage.Get(authSessionId);

        // SEC-022: a session past its maximum age must not be completed, even with
        // a valid TOTP code. Reject it with the same exception as a missing
        // session so "expired" and "not found" are indistinguishable.
        if (DateTimeOffset.Now - twoFactorAuthenticationSession.Created > TwoFactorSessionMaxAge)
        {
            _log.LogWarning(
                "Rejected two-factor session {SessionId} for identity {IdentityId}: session has exceeded its maximum age",
                twoFactorAuthenticationSession.SessionId,
                twoFactorAuthenticationSession.IdentityId);

            throw new TwoFactorAuthSessionNotFoundException("Two factor auth session not found");
        }

        var localAccount = await _localAccountService.GetLocalAccountAsync(twoFactorAuthenticationSession.IdentityId);

        if (localAccount.TwoFactorTotpEnabled is null || localAccount.SharedTotpSecret is null)
        {
            throw new TotpIsNotEnabledForThisAccountException("Totp is not enabled for this local account");
        }

        await _usedTotpLogStorage.PruneStaleAsync(UsedTotpLogMaxAge);

        if (await _usedTotpLogStorage.HasBeenUsedAsync(localAccount.IdentityId, keyCode))
        {
            _log.LogWarning("Rejected replay of an already used totp code for identity {IdentityId}", localAccount.IdentityId);

            throw new TotpCodeAlreadyUsedException($"Totp code already used for identity '{localAccount.IdentityId}'");
        }

        var totp = new Totp(localAccount.SharedTotpSecret);
        if(totp.VerifyTotp(keyCode, out var timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay))
        {
            await _usedTotpLogStorage.CreateLog(new UsedTotpLog { IdentityId = localAccount.IdentityId, Code = keyCode, UsedTime = DateTimeOffset.Now });
            
            var loginResult = await PrivateLogin(host, rememberMe, localAccount);

            return loginResult;
        }

        throw new TotpKeyNotValidException("Totp key not valid");
    }

    private async Task<LoginResult> PrivateLogin(string host, bool rememberMe, LocalAccount localAccount)
    {
        var identity = await _identityService.GetIdentityAsync(localAccount.IdentityId);

        if (identity.Deleted is not null)
        {
            _log.LogWarning("Login blocked for deleted identity {IdentityId}", identity.IdentityId);

            return LoginResult.Failure();
        }

        var expiration = DateTimeOffset.UtcNow.AddHours(1);

        if (rememberMe)
        {
            expiration = DateTimeOffset.UtcNow.AddMonths(1);
        }

        var claims = await GetClaimsForIdentityAsync(identity, host, rememberMe, expiration);

        var authenticationProperties = new AuthenticationProperties()
        {
            AllowRefresh = true,
            ExpiresUtc = expiration,
            IsPersistent = rememberMe,
            IssuedUtc = DateTimeOffset.UtcNow,
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "RinsenPassword", StandardClaims.Name, "role"));

        if (_httpContextAccessor.HttpContext == null)
        {
            throw new Exception("No active http context");
        }

        await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authenticationProperties);

        return LoginResult.Success(principal, localAccount.LoginId);
    }

    private async Task<List<Claim>> GetClaimsForIdentityAsync(Identity identity, string host, bool rememberMe, DateTimeOffset expiration)
    {
        var sessionId = _randomStringGenerator.GetRandomString(32);

        var timeToExpiration = expiration.Subtract(DateTimeOffset.Now);

        var claims = new List<Claim>
        {
            new Claim(StandardClaims.Name, identity.GivenName + " " + identity.Surname, ClaimValueTypes.String, RinsenIdentityConstants.RinsenIdentityProvider),
            new Claim(StandardClaims.FamilyName, identity.Surname, ClaimValueTypes.String, RinsenIdentityConstants.RinsenIdentityProvider),
            new Claim(StandardClaims.GivenName, identity.GivenName, ClaimValueTypes.String, RinsenIdentityConstants.RinsenIdentityProvider),
            new Claim(StandardClaims.Issuer, host, ClaimValueTypes.String,  RinsenIdentityConstants.RinsenIdentityProvider),
            new Claim(StandardClaims.Subject, identity.IdentityId.ToString(), ClaimValueTypes.String, RinsenIdentityConstants.RinsenIdentityProvider),
            new Claim(StandardClaims.SessionId, sessionId, ClaimValueTypes.String, RinsenIdentityConstants.RinsenIdentityProvider),
            new Claim(StandardClaims.Expiration, expiration.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var identityAttributes = await _identityAttributeStorage.GetIdentityAttributesAsync(identity.IdentityId);

        if (identityAttributes.Any(m => m.Attribute == "Administrator"))
        {
            claims.Add(new Claim(RinsenClaimTypes.Administrator, "true", ClaimValueTypes.String, RinsenIdentityConstants.RinsenIdentityProvider));
        }

        return claims;
    }

    /// <summary>
    /// SEC-015: runs a throw-away PBKDF2 with the same cost as a real password
    /// verification (same salt length, iteration count and output length) so that
    /// a login attempt for an unknown account takes the same time as one for a
    /// known account with a wrong password.
    /// </summary>
    private void RunDummyPasswordHash(string password)
    {
        var salt = new byte[_options.NumberOfBytesInPasswordSalt];
        RandomNumberGenerator.Fill(salt);

        _passwordHashGenerator.GetPasswordHash(salt, password, _options.IterationCount, _options.NumberOfBytesInPasswordHash);
    }

    /// <summary>
    /// FUNC-004: signs the user out of the cookie scheme. The cookie handler
    /// removes the authentication cookie and, when a SessionStore is configured
    /// (SqlTicketStore), also removes the server-side ticket — the session is
    /// marked Deleted in the Sessions table.
    /// </summary>
    public async Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            throw new Exception("No active http context");
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
