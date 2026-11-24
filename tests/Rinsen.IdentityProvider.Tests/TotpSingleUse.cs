using System;
using System.Collections.Generic;
using System.Linq;
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
/// SEC-007: TOTP single-use — a code must be rejected when replayed,
/// and stale UsedTotpLog rows must be pruned (RFC 6238).
/// </summary>
public class TotpSingleUseTests
{
    private static readonly Guid IdentityId = Guid.NewGuid();
    private static readonly byte[] TotpSecret = RandomNumberGenerator.GetBytes(20);

    [Fact]
    public async Task ConfirmTotpCode_ValidCode_SucceedsAndLogsTheCode()
    {
        var (loginService, totpLogStorage) = CreateLoginService();
        var code = GenerateValidCode();

        var result = await loginService.ConfirmTotpCode("a-session", code, "localhost", false);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Contains(totpLogStorage.Logs, m => m.IdentityId == IdentityId && m.Code == code);
    }

    [Fact]
    public async Task ConfirmTotpCode_ImmediateReplayOfUsedCode_IsRejected()
    {
        var (loginService, _) = CreateLoginService();
        var code = GenerateValidCode();

        var firstResult = await loginService.ConfirmTotpCode("a-session", code, "localhost", false);

        Assert.True(firstResult.Succeeded);

        var exception = await Assert.ThrowsAsync<TotpCodeAlreadyUsedException>(
            () => loginService.ConfirmTotpCode("a-session", code, "localhost", false));

        // the code is a secret and must not leak into the exception message
        Assert.DoesNotContain(code, exception.Message);
    }

    [Fact]
    public async Task ConfirmTotpCode_InvalidCode_ThrowsTotpKeyNotValidException()
    {
        var (loginService, totpLogStorage) = CreateLoginService();
        var code = GenerateInvalidCode();

        await Assert.ThrowsAsync<TotpKeyNotValidException>(
            () => loginService.ConfirmTotpCode("a-session", code, "localhost", false));

        Assert.Empty(totpLogStorage.Logs);
    }

    [Fact]
    public async Task ConfirmTotpCode_PrunesStaleUsedTotpLogRows()
    {
        var (loginService, totpLogStorage) = CreateLoginService();
        var code = GenerateValidCode();

        await loginService.ConfirmTotpCode("a-session", code, "localhost", false);

        Assert.Contains(TimeSpan.FromMinutes(10), totpLogStorage.PruneCalls);
    }

    private static (LoginService, FakeUsedTotpLogStorage) CreateLoginService()
    {
        var totpLogStorage = new FakeUsedTotpLogStorage();
        var localAccount = new LocalAccount
        {
            IdentityId = IdentityId,
            LoginId = "test-user",
            TwoFactorTotpEnabled = DateTimeOffset.Now,
            SharedTotpSecret = TotpSecret,
        };
        var identity = new Identity
        {
            IdentityId = IdentityId,
            GivenName = "Test",
            Surname = "User",
        };

        var loginService = new LoginService(
            new FakeLocalAccountService(localAccount),
            new FakeIdentityService(identity),
            new FakeIdentityAttributeStorage(),
            totpLogStorage,
            new FakeTwoFactorAuthenticationSessionStorage(IdentityId),
            CreateHttpContextAccessor(),
            new RandomStringGenerator(),
            NullLogger<LoginService>.Instance);

        return (loginService, totpLogStorage);
    }

    private static string GenerateValidCode()
    {
        return new Totp(TotpSecret).ComputeTotp();
    }

    /// <summary>
    /// Deterministically picks a 6-digit code that is not valid for any of the
    /// three time steps covered by the ±1-step verification window.
    /// </summary>
    private static string GenerateInvalidCode()
    {
        var totp = new Totp(TotpSecret);
        var now = DateTime.UtcNow;

        var validCodes = new HashSet<string>
        {
            totp.ComputeTotp(now.AddSeconds(-30)),
            totp.ComputeTotp(now),
            totp.ComputeTotp(now.AddSeconds(30)),
        };

        for (var candidate = 0; candidate < 1_000_000; candidate++)
        {
            var code = candidate.ToString("D6");

            if (!validCodes.Contains(code))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Could not find an invalid totp code");
    }

    private static IHttpContextAccessor CreateHttpContextAccessor()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(new FakeAuthenticationService());

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services.BuildServiceProvider();

        return new FakeHttpContextAccessor(httpContext);
    }

    private class FakeUsedTotpLogStorage : IUsedTotpLogStorage
    {
        private readonly List<UsedTotpLog> _logs = new();

        public List<UsedTotpLog> Logs => _logs;

        public List<TimeSpan> PruneCalls { get; } = new();

        public Task CreateLog(UsedTotpLog usedTotp)
        {
            _logs.Add(usedTotp);

            return Task.CompletedTask;
        }

        public Task<bool> HasBeenUsedAsync(Guid identityId, string code)
        {
            return Task.FromResult(_logs.Any(m => m.IdentityId == identityId && m.Code == code));
        }

        public Task PruneStaleAsync(TimeSpan maxAge)
        {
            PruneCalls.Add(maxAge);

            var cutoff = DateTimeOffset.Now.AddTicks(-maxAge.Ticks);
            _logs.RemoveAll(m => m.UsedTime < cutoff);

            return Task.CompletedTask;
        }
    }

    private class FakeLocalAccountService : ILocalAccountService
    {
        private readonly LocalAccount _localAccount;

        public FakeLocalAccountService(LocalAccount localAccount)
        {
            _localAccount = localAccount;
        }

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

    private class FakeIdentityService : IIdentityService
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

    private class FakeIdentityAttributeStorage : IIdentityAttributeStorage
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

    private class FakeTwoFactorAuthenticationSessionStorage : ITwoFactorAuthenticationSessionStorage
    {
        private readonly TwoFactorAuthenticationSession _session;

        public FakeTwoFactorAuthenticationSessionStorage(Guid identityId)
        {
            _session = new TwoFactorAuthenticationSession
            {
                IdentityId = identityId,
                SessionId = "a-session",
                Type = TwoFactorType.Totp,
            };
        }

        public Task Create(TwoFactorAuthenticationSession twoFactorAuthenticationSession)
        {
            return Task.CompletedTask;
        }

        public Task<TwoFactorAuthenticationSession> Get(string sessionId)
        {
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

    private class FakeHttpContextAccessor : IHttpContextAccessor
    {
        public FakeHttpContextAccessor(HttpContext httpContext)
        {
            HttpContext = httpContext;
        }

        public HttpContext HttpContext { get; set; }
    }

    private class FakeAuthenticationService : IAuthenticationService
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
