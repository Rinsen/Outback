using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OtpNet;
using Rinsen.IdentityProvider.Exceptions;
using Rinsen.IdentityProvider.LocalAccounts;
using Xunit;

namespace Rinsen.IdentityProvider.Tests;

/// <summary>
/// SEC-022: the 2FA session lookup must honor <c>Deleted</c> and age. A
/// logically deleted session is filtered out at the storage layer
/// (<c>AND Deleted IS NULL</c>), and <c>ConfirmTotpCode</c> rejects sessions
/// past their maximum age — even with a valid TOTP code.
/// </summary>
public class TwoFactorSessionSecurityTests
{
    private static readonly Guid IdentityId = Guid.NewGuid();
    private static readonly byte[] TotpSecret = RandomNumberGenerator.GetBytes(20);

    [Fact]
    public async Task ConfirmTotpCode_DeletedSession_IsRejected()
    {
        // The storage layer enforces the "AND Deleted IS NULL" filter: a deleted
        // session is not returned, so Get throws TwoFactorAuthSessionNotFoundException.
        var (loginService, localAccountService, _) = CreateLoginService(
            session: null,
            getException: new TwoFactorAuthSessionNotFoundException("Failed to get session a-session"));

        var code = GenerateValidCode();

        await Assert.ThrowsAsync<TwoFactorAuthSessionNotFoundException>(
            () => loginService.ConfirmTotpCode("a-session", code, "localhost", false));

        Assert.True(localAccountService.GetByIdentityCalls == 0,
            "a deleted session must not proceed to account lookup or login");
    }

    [Fact]
    public async Task ConfirmTotpCode_ExpiredSession_IsRejected()
    {
        // A session created 11 minutes ago is past the 10-minute maximum age.
        var (loginService, localAccountService, totpLogStorage) = CreateLoginService(
            session: CreateSession(created: DateTimeOffset.Now.AddMinutes(-11)));

        var code = GenerateValidCode();

        await Assert.ThrowsAsync<TwoFactorAuthSessionNotFoundException>(
            () => loginService.ConfirmTotpCode("a-session", code, "localhost", false));

        Assert.True(localAccountService.GetByIdentityCalls == 0,
            "an expired session must be rejected before account lookup");
        Assert.True(totpLogStorage.HasBeenUsedCalls == 0,
            "an expired session must be rejected before TOTP verification");
    }

    [Fact]
    public async Task ConfirmTotpCode_FreshSession_ValidCode_Succeeds()
    {
        // Control: a session within the maximum age is still completed.
        var (loginService, _, totpLogStorage) = CreateLoginService(
            session: CreateSession(created: DateTimeOffset.Now.AddMinutes(-9)));

        var code = GenerateValidCode();

        var result = await loginService.ConfirmTotpCode("a-session", code, "localhost", false);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Contains(totpLogStorage.Logs, m => m.IdentityId == IdentityId && m.Code == code);
    }

    [Fact]
    public void SelectSql_FiltersDeletedSessions()
    {
        // The storage SELECT must exclude logically deleted sessions — the only
        // way to verify the filter without a live SQL Server is the statement itself.
        var field = typeof(TwoFactorAuthenticationSessionStorage)
            .GetField("SelectWithSessionId", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(field);

        var sql = (string)field.GetValue(null);
        Assert.Contains("Deleted IS NULL", sql);
    }

    private static (LoginService, FakeLocalAccountService, FakeUsedTotpLogStorage) CreateLoginService(
        TwoFactorAuthenticationSession session,
        Exception getException = null)
    {
        var totpLogStorage = new FakeUsedTotpLogStorage();
        var localAccountService = new FakeLocalAccountService(new LocalAccount
        {
            IdentityId = IdentityId,
            LoginId = "test-user",
            TwoFactorTotpEnabled = DateTimeOffset.Now,
            SharedTotpSecret = TotpSecret,
        });

        var loginService = new LoginService(
            localAccountService,
            new FakeIdentityService(new Identity
            {
                IdentityId = IdentityId,
                GivenName = "Test",
                Surname = "User",
            }),
            new FakeIdentityAttributeStorage(),
            totpLogStorage,
            new FakeTwoFactorSessionStorage(session, getException),
            CreateHttpContextAccessor(),
            new RandomStringGenerator(),
            NullLogger<LoginService>.Instance,
            new IdentityOptions(),
            new PasswordHashGenerator());

        return (loginService, localAccountService, totpLogStorage);
    }

    private static TwoFactorAuthenticationSession CreateSession(DateTimeOffset created)
    {
        return new TwoFactorAuthenticationSession
        {
            IdentityId = IdentityId,
            SessionId = "a-session",
            Type = TwoFactorType.Totp,
            Created = created,
        };
    }

    private static string GenerateValidCode()
    {
        return new Totp(TotpSecret).ComputeTotp();
    }

    private static IHttpContextAccessor CreateHttpContextAccessor()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(new FakeAuthenticationService());

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services.BuildServiceProvider();

        return new FakeHttpContextAccessor(httpContext);
    }

    private sealed class FakeUsedTotpLogStorage : IUsedTotpLogStorage
    {
        private readonly List<UsedTotpLog> _logs = new();

        public List<UsedTotpLog> Logs => _logs;

        public int HasBeenUsedCalls { get; private set; }

        public Task CreateLog(UsedTotpLog usedTotp)
        {
            _logs.Add(usedTotp);

            return Task.CompletedTask;
        }

        public Task<bool> HasBeenUsedAsync(Guid identityId, string code)
        {
            HasBeenUsedCalls++;

            return Task.FromResult(_logs.Any(m => m.IdentityId == identityId && m.Code == code));
        }

        public Task PruneStaleAsync(TimeSpan maxAge)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLocalAccountService : ILocalAccountService
    {
        private readonly LocalAccount _localAccount;

        public FakeLocalAccountService(LocalAccount localAccount)
        {
            _localAccount = localAccount;
        }

        public int GetByIdentityCalls { get; private set; }

        public Task<CreateLocalAccountResult> CreateAsync(Guid identityId, string loginId, string password)
        {
            throw new NotImplementedException();
        }

        public Task<LocalAccount> GetLocalAccountAsync(string loginId, string password)
        {
            return Task.FromResult(_localAccount);
        }

        public Task<LocalAccount> GetLocalAccountAsync(Guid identityId)
        {
            GetByIdentityCalls++;

            return Task.FromResult(_localAccount);
        }

        public Task ChangePasswordAsync(string oldPassword, string newPassword)
        {
            throw new NotImplementedException();
        }

        public Task DeleteLocalAccountAsync(string password)
        {
            throw new NotImplementedException();
        }

        public Task ValidatePasswordAsync(string password)
        {
            throw new NotImplementedException();
        }

        public Task<string> EnableTotp()
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeIdentityService : IIdentityService
    {
        private readonly Identity _identity;

        public FakeIdentityService(Identity identity)
        {
            _identity = identity;
        }

        public Task<Identity> GetIdentityAsync(Guid identityId)
        {
            return Task.FromResult(_identity);
        }

        public Task<Identity> GetIdentityAsync()
        {
            return Task.FromResult(_identity);
        }

        public void UpdateIdentityDetails(string firstName, string lastName, string email, string phoneNumber)
        {
            throw new NotImplementedException();
        }

        public Task<CreateIdentityResult> CreateAsync(string firstName, string lastName, string email, string phoneNumber)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeIdentityAttributeStorage : IIdentityAttributeStorage
    {
        public Task<IEnumerable<IdentityAttribute>> GetIdentityAttributesAsync(Guid identityId)
        {
            return Task.FromResult(Enumerable.Empty<IdentityAttribute>());
        }

        public Task CreateAsync(Identity identity, string attribute, string value = "")
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(IdentityAttribute identityAttribute)
        {
            throw new NotImplementedException();
        }

        public Task<int> CountAdministratorsAsync()
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeTwoFactorSessionStorage : ITwoFactorAuthenticationSessionStorage
    {
        private readonly TwoFactorAuthenticationSession _session;
        private readonly Exception _getException;

        public FakeTwoFactorSessionStorage(TwoFactorAuthenticationSession session, Exception getException = null)
        {
            _session = session;
            _getException = getException;
        }

        public Task Create(TwoFactorAuthenticationSession twoFactorAuthenticationSession)
        {
            return Task.CompletedTask;
        }

        public Task<TwoFactorAuthenticationSession> Get(string sessionId)
        {
            if (_getException is not null)
            {
                throw _getException;
            }

            return Task.FromResult(_session);
        }

        public Task Update(TwoFactorAuthenticationSession twoFactorAuthenticationSession)
        {
            return Task.CompletedTask;
        }

        public Task Delete(TwoFactorAuthenticationSession twoFactorAuthenticationSession)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHttpContextAccessor : IHttpContextAccessor
    {
        public FakeHttpContextAccessor(HttpContext httpContext)
        {
            HttpContext = httpContext;
        }

        public HttpContext HttpContext { get; set; }
    }

    private sealed class FakeAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal user, AuthenticationProperties properties)
        {
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties)
        {
            return Task.CompletedTask;
        }

        public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties)
        {
            return Task.CompletedTask;
        }

        public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties)
        {
            return Task.CompletedTask;
        }
    }
}
