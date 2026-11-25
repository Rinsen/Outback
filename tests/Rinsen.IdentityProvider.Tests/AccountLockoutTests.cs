using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Rinsen.IdentityProvider;
using Rinsen.IdentityProvider.Exceptions;
using Rinsen.IdentityProvider.LocalAccounts;
using Xunit;

namespace Rinsen.IdentityProvider.Tests
{
    /// <summary>
    /// SEC-016: once <c>FailedLoginCount</c> reaches <c>MaxFailedLoginSleepCount</c>
    /// the account is locked out. The N+1th attempt is blocked even when the correct
    /// password is supplied, and the login path reports the same generic failure as a
    /// wrong password so the lockout is not distinguishable from a bad credential.
    /// </summary>
    public class AccountLockoutTests
    {
        private static readonly Guid IdentityId = Guid.NewGuid();
        private const string LoginId = "locked@example.com";
        private const string Password = "correct-horse-battery-staple";
        private const string WrongPassword = "wrong-password-entirely";

        [Fact]
        public async Task GetLocalAccountAsync_NFailedThenCorrectPassword_IsLockedOut()
        {
            var (localAccountService, account, options) = CreateAccount();

            // N failed attempts: each throws InvalidPasswordException and
            // increments FailedLoginCount on the same stored instance.
            for (var i = 0; i < options.MaxFailedLoginSleepCount; i++)
            {
                await Assert.ThrowsAsync<InvalidPasswordException>(
                    () => localAccountService.GetLocalAccountAsync(LoginId, WrongPassword));
            }

            Assert.Equal(options.MaxFailedLoginSleepCount, account.FailedLoginCount);

            // N+1th attempt: even with the correct password the account is locked.
            await Assert.ThrowsAsync<AccountLockedOutException>(
                () => localAccountService.GetLocalAccountAsync(LoginId, Password));
        }

        [Fact]
        public async Task GetLocalAccountAsync_PreLockedAccount_CorrectPassword_IsLockedOut()
        {
            var (localAccountService, account, options) = CreateAccount();

            // Simulate an account that is already past the threshold.
            account.FailedLoginCount = options.MaxFailedLoginSleepCount;

            await Assert.ThrowsAsync<AccountLockedOutException>(
                () => localAccountService.GetLocalAccountAsync(LoginId, Password));
        }

        [Fact]
        public async Task LoginAsync_LockedOutAccount_CorrectPassword_ReturnsGenericFailure()
        {
            var (localAccountService, account, options) = CreateAccount();

            var loginService = new LoginService(
                localAccountService,
                new StubIdentityService(),
                new StubIdentityAttributeStorage(),
                new StubUsedTotpLogStorage(),
                new StubTwoFactorSessionStorage(),
                new StubHttpContextAccessor(),
                new RandomStringGenerator(),
                NullLogger<LoginService>.Instance,
                options,
                new PasswordHashGenerator());

            // Lock the account out with N failed attempts.
            for (var i = 0; i < options.MaxFailedLoginSleepCount; i++)
            {
                var failed = await loginService.LoginAsync(LoginId, WrongPassword, "localhost", false);
                Assert.True(failed.Failed);
            }

            // N+1th attempt with the correct password is blocked with the same
            // generic failure as a wrong password (no principal, no 2FA session).
            var result = await loginService.LoginAsync(LoginId, Password, "localhost", false);
            Assert.True(result.Failed);
            Assert.True(result.Principal is null, "a locked-out login must not produce a principal");
        }

        private static (LocalAccountService, LocalAccount, IdentityOptions) CreateAccount()
        {
            // A small iteration count keeps the (real) KDF cheap; the lockout
            // logic is independent of the hashing cost.
            var options = new IdentityOptions
            {
                MaxFailedLoginSleepCount = 3,
                IterationCount = 10_000
            };

            var storage = new FakeLocalAccountStorage(null);

            var localAccountService = new LocalAccountService(
                new FakeIdentityAccessor(IdentityId),
                storage,
                options,
                new PasswordHashGenerator(),
                NullLogger<LocalAccountService>.Instance);

            var created = localAccountService.CreateAsync(IdentityId, LoginId, Password).GetAwaiter().GetResult();
            Assert.True(created.Succeeded);

            return (localAccountService, created.LocalAccount, options);
        }

        /// <summary>
        /// Mimics <c>LocalAccountStorage</c> by returning a stable <see cref="LocalAccount"/>
        /// instance so <c>FailedLoginCount</c> accumulates in memory; the persistence
        /// methods are no-ops.
        /// </summary>
        private sealed class FakeLocalAccountStorage : ILocalAccountStorage
        {
            private LocalAccount _localAccount;

            public FakeLocalAccountStorage(LocalAccount localAccount)
            {
                _localAccount = localAccount;
            }

            public Task CreateAsync(LocalAccount localAccount)
            {
                _localAccount = localAccount;
                return Task.CompletedTask;
            }

            public Task<LocalAccount> GetAsync(Guid identityId)
            {
                if (_localAccount is null || _localAccount.IdentityId != identityId)
                {
                    throw new AccountNotFoundException($"Identity {identityId} not found");
                }

                return Task.FromResult(_localAccount);
            }

            public Task<LocalAccount> GetAsync(string loginId)
            {
                if (_localAccount is null || _localAccount.LoginId != loginId)
                {
                    throw new AccountNotFoundException($"Could not find local account for {loginId}");
                }

                return Task.FromResult(_localAccount);
            }

            public Task<List<LocalAccount>> GetAllAsync()
            {
                return Task.FromResult(new List<LocalAccount> { _localAccount });
            }

            public Task UpdateAsync(LocalAccount localAccount)
            {
                return Task.CompletedTask;
            }

            public Task DeleteAsync(LocalAccount localAccount)
            {
                return Task.CompletedTask;
            }

            public Task UpdateFailedLoginCountAsync(LocalAccount localAccount)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class FakeIdentityAccessor : IIdentityAccessor
        {
            public FakeIdentityAccessor(Guid identityId)
            {
                IdentityId = identityId;
            }

            public ClaimsPrincipal ClaimsPrincipal { get; }
            public Guid IdentityId { get; }
        }

        // The login-failure paths under test never reach these dependencies;
        // the stubs exist only to satisfy the constructor.

        private sealed class StubIdentityService : IIdentityService
        {
            public Task<Identity> GetIdentityAsync(Guid identityId) => throw new NotImplementedException();
            public Task<Identity> GetIdentityAsync() => throw new NotImplementedException();
            public void UpdateIdentityDetails(string firstName, string lastName, string email, string phoneNumber) => throw new NotImplementedException();
            public Task<CreateIdentityResult> CreateAsync(string firstName, string lastName, string email, string phoneNumber) => throw new NotImplementedException();
        }

        private sealed class StubIdentityAttributeStorage : IIdentityAttributeStorage
        {
            public Task<IEnumerable<IdentityAttribute>> GetIdentityAttributesAsync(Guid identityId) => throw new NotImplementedException();
            public Task CreateAsync(Identity identity, string attribute, string value = "") => throw new NotImplementedException();
            public Task DeleteAsync(IdentityAttribute identityAttribute) => throw new NotImplementedException();
            public Task<int> CountAdministratorsAsync() => throw new NotImplementedException();
        }

        private sealed class StubUsedTotpLogStorage : IUsedTotpLogStorage
        {
            public Task CreateLog(UsedTotpLog usedTotp) => throw new NotImplementedException();
            public Task<bool> HasBeenUsedAsync(Guid identityId, string code) => throw new NotImplementedException();
            public Task PruneStaleAsync(TimeSpan maxAge) => throw new NotImplementedException();
        }

        private sealed class StubTwoFactorSessionStorage : ITwoFactorAuthenticationSessionStorage
        {
            public Task Create(TwoFactorAuthenticationSession twoFactorAuthenticationSession) => throw new NotImplementedException();
            public Task<TwoFactorAuthenticationSession> Get(string sessionId) => throw new NotImplementedException();
            public Task Update(TwoFactorAuthenticationSession twoFactorAuthenticationSession) => throw new NotImplementedException();
            public Task Delete(TwoFactorAuthenticationSession twoFactorAuthenticationSession) => throw new NotImplementedException();
        }

        private sealed class StubHttpContextAccessor : IHttpContextAccessor
        {
            public HttpContext HttpContext { get; set; }
        }
    }
}
