using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rinsen.IdentityProvider;
using Rinsen.IdentityProvider.LocalAccounts;
using Rinsen.Outback.Claims;
using Xunit;

namespace Rinsen.IdentityProvider.Tests
{
    /// <summary>
    /// FUNC-004: LoginService.LogoutAsync must sign the user out and remove the
    /// server-side ticket (the session persisted by SqlTicketStore).
    /// </summary>
    public class LogoutTests
    {
        private static readonly Guid IdentityId = Guid.NewGuid();
        private const string SessionId = "logout-test-session";

        [Fact]
        public async Task LogoutAsync_SignsOutAndInvalidatesServerSideSession()
        {
            // Arrange
            var sessionStorage = new FakeSessionStorage();
            var server = CreateServer(sessionStorage);
            using var client = server.CreateClient();

            // Act 1: sign in — the ticket store persists a server-side session
            var loginResponse = await client.GetAsync("/login");

            // Assert: the session exists and is active
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
            // The cookie holds a data-protected ticket with the session key embedded
            // in its properties; the handler recovers the key on sign-out. Replay the
            // cookie value verbatim, exactly as a browser would.
            var setCookie = Assert.Single(loginResponse.Headers.GetValues("Set-Cookie"));

            var activeSession = await sessionStorage.GetAsync(SessionId);
            Assert.NotNull(activeSession);
            Assert.Null(activeSession.Deleted);

            var cookiePair = setCookie.Split(';', 2)[0].Trim();
            var cookieName = cookiePair.Split('=', 2)[0].Trim();
            var cookieValue = cookiePair.Split('=', 2)[1].Trim();

            // Act 2: log out through LoginService, replaying the authentication cookie.
            // TestServer's client handler does not persist cookies between requests,
            // so the cookie is sent explicitly — like a browser would.
            var logoutRequest = new HttpRequestMessage(HttpMethod.Get, "/logout");
            logoutRequest.Headers.Add("Cookie", $"{cookieName}={cookieValue}");
            var logoutResponse = await client.SendAsync(logoutRequest);

            // Assert: signed out (cookie removed) and the server-side session is invalidated
            Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
            Assert.True(logoutResponse.Headers.Contains("Set-Cookie"), "sign-out must clear the authentication cookie");

            Assert.True(await sessionStorage.GetAsync(SessionId) is null, "the active session lookup must no longer find the session");

            var stored = await sessionStorage.GetAsync(SessionId, includeDeleted: true);
            Assert.NotNull(stored);
            Assert.True(stored.Deleted is not null, "the session must be marked Deleted, like SessionStorage.DeleteSql does");
        }

        [Fact]
        public async Task LogoutAsync_WhenNotSignedIn_DoesNotThrow()
        {
            // Arrange
            var sessionStorage = new FakeSessionStorage();
            var server = CreateServer(sessionStorage);
            using var client = server.CreateClient();

            // Act
            var logoutResponse = await client.GetAsync("/logout");

            // Assert
            Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
            Assert.Empty(sessionStorage.AllSessions);
        }

        private static TestServer CreateServer(FakeSessionStorage sessionStorage)
        {
            var httpContextAccessor = new HttpContextAccessor();
            var ticketStore = new SqlTicketStore(sessionStorage, httpContextAccessor);

            return new TestServer(
                new WebHostBuilder()
                    .ConfigureServices(services =>
                    {
                        services.AddLogging();
                        services.AddRouting();
                        services.AddDataProtection();
                        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
                        services.AddScoped<ISessionStorage>(sp => sessionStorage);

                        // Mirrors Rinsen.Outback.App's AddAuthentication: cookie scheme with
                        // a server-side ticket store.
                        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                            .AddCookie(options =>
                            {
                                options.SessionStore = ticketStore;
                            });

                        services.AddScoped<ILoginService, LoginService>();
                        services.AddScoped<ILocalAccountService, StubLocalAccountService>();
                        services.AddScoped<IIdentityService, StubIdentityService>();
                        services.AddScoped<IIdentityAttributeStorage, StubIdentityAttributeStorage>();
                        services.AddScoped<IUsedTotpLogStorage, StubUsedTotpLogStorage>();
                        services.AddScoped<ITwoFactorAuthenticationSessionStorage, StubTwoFactorSessionStorage>();
                        services.AddSingleton<RandomStringGenerator>();
                        services.AddSingleton(new IdentityOptions());
                        services.AddScoped<PasswordHashGenerator>();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthentication();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/login", async context =>
                            {
                                await context.SignInAsync(
                                    CookieAuthenticationDefaults.AuthenticationScheme,
                                    CreatePrincipal(),
                                    new AuthenticationProperties
                                    {
                                        IsPersistent = false,
                                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1),
                                    });

                                context.Response.StatusCode = 200;
                            });

                            endpoints.MapGet("/logout", async context =>
                            {
                                var loginService = context.RequestServices.GetRequiredService<ILoginService>();
                                await loginService.LogoutAsync();
                                context.Response.StatusCode = 200;
                            });
                        });
                    }));
        }

        private static ClaimsPrincipal CreatePrincipal()
        {
            var claims = new List<Claim>
            {
                new Claim(StandardClaims.Subject, IdentityId.ToString(), ClaimValueTypes.String),
                new Claim(StandardClaims.SessionId, SessionId, ClaimValueTypes.String),
                new Claim(StandardClaims.Expiration, DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            };

            return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }

        /// <summary>
        /// In-memory mimic of <see cref="SessionStorage"/>: DeleteAsync marks the
        /// session Deleted (like the real <c>UPDATE Sessions SET Deleted = …</c>),
        /// and GetAsync without includeDeleted filters deleted sessions out.
        /// </summary>
        private sealed class FakeSessionStorage : ISessionStorage
        {
            private readonly ConcurrentDictionary<string, Session> _sessions = new ConcurrentDictionary<string, Session>();

            public ICollection<Session> AllSessions => _sessions.Values;

            public Task CreateAsync(Session session)
            {
                _sessions[session.SessionId] = session;
                return Task.CompletedTask;
            }

            public Task DeleteAsync(string sessionId)
            {
                if (_sessions.TryGetValue(sessionId, out var session))
                {
                    session.Deleted = DateTimeOffset.Now;
                }

                return Task.CompletedTask;
            }

            public Task<IEnumerable<Session>> GetAsync(Guid identityId, bool includeDeleted = false)
            {
                var result = new List<Session>();
                foreach (var session in _sessions.Values)
                {
                    if (session.IdentityId == identityId && (includeDeleted || session.Deleted is null))
                    {
                        result.Add(session);
                    }
                }

                return Task.FromResult<IEnumerable<Session>>(result);
            }

            public Task<Session> GetAsync(string sessionId, bool includeDeleted = false)
            {
                if (_sessions.TryGetValue(sessionId, out var session) && (includeDeleted || session.Deleted is null))
                {
                    return Task.FromResult(session);
                }

                return Task.FromResult<Session>(null);
            }

            public Task UpdateAsync(Session session)
            {
                _sessions[session.SessionId] = session;
                return Task.CompletedTask;
            }
        }

        // The logout path never reaches these dependencies; the stubs exist only
        // to satisfy the LoginService constructor.

        private sealed class StubLocalAccountService : ILocalAccountService
        {
            public Task<CreateLocalAccountResult> CreateAsync(Guid identityId, string loginId, string password) => throw new NotImplementedException();
            public Task<LocalAccount> GetLocalAccountAsync(string loginId, string password) => throw new NotImplementedException();
            public Task<LocalAccount> GetLocalAccountAsync(Guid identityId) => throw new NotImplementedException();
            public Task ChangePasswordAsync(string oldPassword, string newPassword) => throw new NotImplementedException();
            public Task DeleteLocalAccountAsync(string password) => throw new NotImplementedException();
            public Task ValidatePasswordAsync(string password) => throw new NotImplementedException();
            public Task<string> EnableTotp() => throw new NotImplementedException();
        }

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
    }
}
