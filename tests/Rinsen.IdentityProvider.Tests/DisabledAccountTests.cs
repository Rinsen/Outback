using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Rinsen.IdentityProvider;
using Rinsen.IdentityProvider.Exceptions;
using Rinsen.IdentityProvider.LocalAccounts;
using Xunit;

namespace Rinsen.IdentityProvider.Tests
{
    /// <summary>
    /// SEC-017: a disabled account must not authenticate. <c>LoginAsync</c> rejects
    /// the login with the same generic failure as a wrong password (so the state is
    /// not distinguishable), including for accounts with TOTP enabled, and an enabled
    /// account still signs in successfully.
    /// </summary>
    public class DisabledAccountTests
    {
        private static readonly Guid IdentityId = Guid.NewGuid();
        private const string LoginId = "disabled@example.com";
        private const string Password = "correct-horse-battery-staple";

        [Fact]
        public async Task LoginAsync_DisabledAccount_ReturnsGenericFailure()
        {
            var (loginService, account) = CreateLoginService();

            account.IsDisabled = true;

            var result = await loginService.LoginAsync(LoginId, Password, "localhost", false);

            Assert.True(result.Failed, "a disabled account must not authenticate");
            Assert.True(result.Principal is null, "a disabled login must not produce a principal");
            Assert.True(!result.TwoFactorRequired, "a disabled login must not start a 2FA flow");
        }

        [Fact]
        public async Task LoginAsync_DisabledAccountWithTotpEnabled_ReturnsGenericFailure()
        {
            var (loginService, account) = CreateLoginService();

            // TOTP enabled on the account: the IsDisabled rejection must still run
            // before the 2FA branch, so no 2FA session is started either.
            account.IsDisabled = true;
            account.TwoFactorTotpEnabled = DateTimeOffset.Now;

            var result = await loginService.LoginAsync(LoginId, Password, "localhost", false);

            Assert.True(result.Failed, "a disabled account must not authenticate even with TOTP enabled");
            Assert.True(result.Principal is null, "a disabled login must not produce a principal");
            Assert.True(!result.TwoFactorRequired, "a disabled login must not start a 2FA flow");
        }

        [Fact]
        public async Task LoginAsync_EnabledAccount_SignsIn()
        {
            // Arrange: full host with cookie authentication, the real
            // LocalAccountService (real password verification) and LoginService.
            var storage = new FakeLocalAccountStorage(null);

            var options = new IdentityOptions
            {
                IterationCount = 10_000
            };

            var localAccountService = new LocalAccountService(
                new FakeIdentityAccessor(IdentityId),
                storage,
                options,
                new PasswordHashGenerator(),
                NullLogger<LocalAccountService>.Instance);

            var created = localAccountService.CreateAsync(IdentityId, LoginId, Password).GetAwaiter().GetResult();
            Assert.True(created.Succeeded);
            Assert.True(!created.LocalAccount.IsDisabled, "the control account must be enabled");

            var httpContextAccessor = new HttpContextAccessor();

            var server = new TestServer(
                new WebHostBuilder()
                    .ConfigureServices(services =>
                    {
                        services.AddLogging();
                        services.AddRouting();
                        services.AddDataProtection();
                        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);

                        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                            .AddCookie();

                        services.AddScoped<ILoginService, LoginService>();
                        services.AddScoped<ILocalAccountService, LocalAccountService>();
                        services.AddScoped<IIdentityAccessor>(sp => new FakeIdentityAccessor(IdentityId));
                        services.AddScoped<ILocalAccountStorage>(sp => storage);
                        services.AddScoped<IIdentityService, EnabledIdentityService>();
                        services.AddScoped<IIdentityAttributeStorage, NoAttributesStorage>();
                        services.AddScoped<IUsedTotpLogStorage, StubUsedTotpLogStorage>();
                        services.AddScoped<ITwoFactorAuthenticationSessionStorage, StubTwoFactorSessionStorage>();
                        services.AddSingleton<RandomStringGenerator>();
                        services.AddSingleton(options);
                        services.AddScoped<PasswordHashGenerator>();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthentication();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapPost("/login", async context =>
                            {
                                var loginService = context.RequestServices.GetRequiredService<ILoginService>();
                                var result = await loginService.LoginAsync(LoginId, Password, "localhost", false);

                                context.Response.StatusCode = result.Succeeded ? 200 : 401;
                            });
                        });
                    }));

            using var client = server.CreateClient();

            // Act
            var response = await client.PostAsync("/login", null);

            // Assert: the enabled account authenticates and a sign-in cookie is issued
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.Contains("Set-Cookie"), "a successful login must issue the authentication cookie");
        }

        private static (LoginService, LocalAccount) CreateLoginService()
        {
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

            return (loginService, created.LocalAccount);
        }

        /// <summary>
        /// Mimics <c>LocalAccountStorage</c> by returning a stable <see cref="LocalAccount"/>
        /// instance; the persistence methods are no-ops.
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

        // The disabled-login paths under test never reach these dependencies;
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

        // Success-path stubs: the enabled account reaches PrivateLogin, which
        // needs a non-deleted identity and an attribute list.

        private sealed class NoAttributesStorage : IIdentityAttributeStorage
        {
            public Task<IEnumerable<IdentityAttribute>> GetIdentityAttributesAsync(Guid identityId)
            {
                return Task.FromResult<IEnumerable<IdentityAttribute>>(new List<IdentityAttribute>());
            }

            public Task CreateAsync(Identity identity, string attribute, string value = "") => throw new NotImplementedException();
            public Task DeleteAsync(IdentityAttribute identityAttribute) => throw new NotImplementedException();
            public Task<int> CountAdministratorsAsync() => throw new NotImplementedException();
        }

        private sealed class EnabledIdentityService : IIdentityService
        {
            public Task<Identity> GetIdentityAsync(Guid identityId)
            {
                return Task.FromResult(new Identity
                {
                    IdentityId = identityId,
                    GivenName = "Jane",
                    Surname = "Doe",
                    Email = "jane.doe@example.com",
                    EmailConfirmed = true,
                    Deleted = null
                });
            }

            public Task<Identity> GetIdentityAsync() => throw new NotImplementedException();
            public void UpdateIdentityDetails(string firstName, string lastName, string email, string phoneNumber) => throw new NotImplementedException();
            public Task<CreateIdentityResult> CreateAsync(string firstName, string lastName, string email, string phoneNumber) => throw new NotImplementedException();
        }
    }
}
