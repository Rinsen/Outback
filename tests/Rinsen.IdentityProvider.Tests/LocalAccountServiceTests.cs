using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rinsen.IdentityProvider;
using Rinsen.IdentityProvider.Exceptions;
using Rinsen.IdentityProvider.LocalAccounts;
using Xunit;

namespace Rinsen.IdentityProvider.Tests
{
    /// <summary>
    /// SEC-014: the default PBKDF2 iteration count must meet the OWASP minimum
    /// (600,000 for HMAC-SHA256), and accounts created with a legacy per-account
    /// iteration count must still validate against their stored hash.
    /// </summary>
    public class LocalAccountServiceTests
    {
        private static readonly Guid IdentityId = Guid.NewGuid();
        private const string LoginId = "test-login";
        private const string Password = "correct-horse-battery-staple";
        private const string WrongPassword = "wrong-password-entirely";

        [Fact]
        public void IdentityOptions_DefaultIterationCount_IsAtLeastOwaspMinimum()
        {
            var options = new IdentityOptions();

            Assert.True(options.IterationCount >= 600_000,
                $"default iteration count {options.IterationCount} is below the OWASP minimum of 600000");
        }

        [Fact]
        public async Task CreateAsync_NewAccount_UsesDefaultIterationCount()
        {
            var storage = new InMemoryLocalAccountStorage();
            var service = CreateService(storage, new IdentityOptions());

            var result = await service.CreateAsync(IdentityId, LoginId, Password);

            Assert.True(result.Succeeded);
            Assert.True(result.LocalAccount.IterationCount >= 600_000,
                $"new account stored with iteration count {result.LocalAccount.IterationCount}, below the OWASP minimum");
        }

        [Fact]
        public async Task GetLocalAccountAsync_LegacyPerAccountIterationCount_StillValidates()
        {
            // Account created under the pre-SEC-014 default of 100,000 iterations.
            var storage = new InMemoryLocalAccountStorage();
            var legacyService = CreateService(storage, new IdentityOptions { IterationCount = 100_000 });
            var result = await legacyService.CreateAsync(IdentityId, LoginId, Password);
            Assert.True(result.Succeeded);
            Assert.Equal(100_000, result.LocalAccount.IterationCount);

            // A service configured with the new default must still validate the
            // legacy account's stored hash — verification is per-account, not per-service.
            var currentService = CreateService(storage, new IdentityOptions());

            var account = await currentService.GetLocalAccountAsync(LoginId, Password);

            Assert.Equal(IdentityId, account.IdentityId);
        }

        [Fact]
        public async Task GetLocalAccountAsync_WrongPassword_ThrowsInvalidPasswordException()
        {
            var storage = new InMemoryLocalAccountStorage();
            var service = CreateService(storage, new IdentityOptions());
            await service.CreateAsync(IdentityId, LoginId, Password);

            await Assert.ThrowsAsync<InvalidPasswordException>(() =>
                service.GetLocalAccountAsync(LoginId, WrongPassword));
        }

        private static LocalAccountService CreateService(ILocalAccountStorage storage, IdentityOptions options)
        {
            return new LocalAccountService(
                new FakeIdentityAccessor(IdentityId),
                storage,
                options,
                new PasswordHashGenerator(),
                new LoggerFactory().CreateLogger<LocalAccountService>());
        }

        private sealed class FakeIdentityAccessor : IIdentityAccessor
        {
            public FakeIdentityAccessor(Guid identityId)
            {
                IdentityId = identityId;
            }

            public System.Security.Claims.ClaimsPrincipal ClaimsPrincipal { get; }
            public Guid IdentityId { get; }
        }

        private sealed class InMemoryLocalAccountStorage : ILocalAccountStorage
        {
            private readonly Dictionary<Guid, LocalAccount> _byIdentity = new();
            private readonly Dictionary<string, LocalAccount> _byLoginId = new();

            public Task CreateAsync(LocalAccount localAccount)
            {
                _byIdentity[localAccount.IdentityId] = localAccount;
                _byLoginId[localAccount.LoginId] = localAccount;
                return Task.CompletedTask;
            }

            public Task<LocalAccount> GetAsync(Guid identityId)
            {
                return Task.FromResult(_byIdentity[identityId]);
            }

            public Task<LocalAccount> GetAsync(string loginId)
            {
                return Task.FromResult(_byLoginId[loginId]);
            }

            public Task<List<LocalAccount>> GetAllAsync()
            {
                return Task.FromResult(new List<LocalAccount>(_byIdentity.Values));
            }

            public Task UpdateAsync(LocalAccount localAccount)
            {
                return Task.CompletedTask;
            }

            public Task DeleteAsync(LocalAccount localAccount)
            {
                _byIdentity.Remove(localAccount.IdentityId);
                _byLoginId.Remove(localAccount.LoginId);
                return Task.CompletedTask;
            }

            public Task UpdateFailedLoginCountAsync(LocalAccount localAccount)
            {
                return Task.CompletedTask;
            }
        }
    }
}
