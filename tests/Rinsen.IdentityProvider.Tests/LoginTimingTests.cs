using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// SEC-015: a login attempt for an unknown account must run a dummy KDF with
    /// the same cost as a real verification, so that unknown and known accounts
    /// are not distinguishable by response time (account enumeration).
    /// </summary>
    public class LoginTimingTests
    {
        private static readonly Guid IdentityId = Guid.NewGuid();
        private const string KnownLoginId = "known@example.com";
        private const string UnknownLoginId = "unknown@example.com";
        private const string Password = "correct-horse-battery-staple";
        private const string WrongPassword = "wrong-password-entirely";

        [Fact]
        public async Task LoginAsync_UnknownAccount_RunsDummyKdf()
        {
            var (loginService, spy) = CreateLoginService(knownAccount: null);

            var result = await loginService.LoginAsync(UnknownLoginId, Password, "localhost", false);

            Assert.True(result.Failed);
            Assert.True(spy.CallCount == 1, "unknown-account path must perform exactly one KDF (the dummy)");
            Assert.True(spy.LastIterationCount == new IdentityOptions().IterationCount,
                $"dummy KDF used {spy.LastIterationCount} iterations, expected the configured default");
        }

        [Fact]
        public async Task LoginAsync_KnownAccountWrongPassword_RunsKdf()
        {
            var (loginService, spy) = CreateLoginService(knownAccount: CreateKnownAccount());

            var result = await loginService.LoginAsync(KnownLoginId, WrongPassword, "localhost", false);

            Assert.True(result.Failed);
            Assert.True(spy.CallCount == 1, "known-account path must perform exactly one KDF (the real verification)");
            Assert.True(spy.LastIterationCount == new IdentityOptions().IterationCount,
                $"verification KDF used {spy.LastIterationCount} iterations, expected the configured default");
        }

        [Fact]
        public async Task LoginAsync_UnknownAccount_TakesComparableTimeToKnownAccount()
        {
            var unknown = CreateLoginService(knownAccount: null);
            var known = CreateLoginService(knownAccount: CreateKnownAccount());

            // Warm up both paths (JIT, crypto provider initialization).
            await unknown.Item1.LoginAsync(UnknownLoginId, Password, "localhost", false);
            await known.Item1.LoginAsync(KnownLoginId, WrongPassword, "localhost", false);

            var unknownDuration = MedianDuration(() => unknown.Item1.LoginAsync(UnknownLoginId, Password, "localhost", false));
            var knownDuration = MedianDuration(() => known.Item1.LoginAsync(KnownLoginId, WrongPassword, "localhost", false));

            Assert.True(unknownDuration > knownDuration * 0.5,
                $"unknown-account path ({unknownDuration.TotalMilliseconds:0.0} ms) is much faster than the known-account path ({knownDuration.TotalMilliseconds:0.0} ms) — the dummy KDF is missing");
            Assert.True(unknownDuration < knownDuration * 2.0,
                $"unknown-account path ({unknownDuration.TotalMilliseconds:0.0} ms) is much slower than the known-account path ({knownDuration.TotalMilliseconds:0.0} ms)");
        }

        private static (LoginService, CountingPasswordHashGenerator) CreateLoginService(LocalAccount knownAccount)
        {
            var options = new IdentityOptions();
            var spy = new CountingPasswordHashGenerator();
            var storage = new FakeLocalAccountStorage(knownAccount);

            var localAccountService = new LocalAccountService(
                new FakeIdentityAccessor(IdentityId),
                storage,
                options,
                spy,
                NullLogger<LocalAccountService>.Instance);

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
                spy);

            return (loginService, spy);
        }

        /// <summary>
        /// Creates a known account through the real <see cref="LocalAccountService"/>
        /// so its stored hash is produced exactly as in production.
        /// </summary>
        private static LocalAccount CreateKnownAccount()
        {
            var options = new IdentityOptions();
            var storage = new FakeLocalAccountStorage(null);

            var localAccountService = new LocalAccountService(
                new FakeIdentityAccessor(IdentityId),
                storage,
                options,
                new PasswordHashGenerator(),
                NullLogger<LocalAccountService>.Instance);

            var result = localAccountService.CreateAsync(IdentityId, KnownLoginId, Password).GetAwaiter().GetResult();

            Assert.True(result.Succeeded);

            return result.LocalAccount;
        }

        private static TimeSpan MedianDuration(Func<Task> action, int runs = 3)
        {
            var durations = new List<TimeSpan>();

            for (var i = 0; i < runs; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                action().GetAwaiter().GetResult();
                stopwatch.Stop();
                durations.Add(stopwatch.Elapsed);
            }

            durations.Sort();

            return durations[durations.Count / 2];
        }

        /// <summary>
        /// Counts KDF invocations and records the iteration count of the last one,
        /// delegating to the real implementation so the cost is genuine.
        /// </summary>
        private sealed class CountingPasswordHashGenerator : PasswordHashGenerator
        {
            public int CallCount { get; private set; }

            public int LastIterationCount { get; private set; }

            internal override byte[] GetPasswordHash(byte[] passwordSalt, string password, int iterationCount, int numBytesRequested)
            {
                CallCount++;
                LastIterationCount = iterationCount;

                return base.GetPasswordHash(passwordSalt, password, iterationCount, numBytesRequested);
            }
        }

        /// <summary>
        /// Mimics <c>LocalAccountStorage.GetAsync(string)</c>: returns the stored
        /// account or throws <see cref="AccountNotFoundException"/>.
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
