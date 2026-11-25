using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rinsen.IdentityProvider;
using Rinsen.IdentityProvider.AuditLogging;
using Rinsen.IdentityProvider.LocalAccounts;
using Rinsen.Outback.App.Controllers;
using Rinsen.Outback.App.Models;
using Rinsen.Outback.Helpers;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// SEC-020: the anonymous-registration invite code must be a strong random
    /// value (>= 128 bits). A missing or weak configured code fails closed —
    /// self-registration is disabled — with the same signature as a wrong code.
    /// </summary>
    public class InviteCodePolicyTests
    {
        [Fact]
        public void IsStrong_Null_IsFalse()
        {
            Assert.False(InviteCodePolicy.IsStrong(null));
        }

        [Fact]
        public void IsStrong_ShortCodes_AreFalse()
        {
            Assert.False(InviteCodePolicy.IsStrong("1234"));
            Assert.False(InviteCodePolicy.IsStrong(new string('a', 31)));
        }

        [Fact]
        public void IsStrong_128BitsOrLonger_IsTrue()
        {
            Assert.True(InviteCodePolicy.IsStrong(new string('a', 32)));
            Assert.True(InviteCodePolicy.IsStrong(new string('a', 64)));
        }
    }

    /// <summary>
    /// SEC-020: the anonymous-registration endpoint (IdentityController.Create)
    /// enforces the invite-code policy. The constant-time comparison
    /// (SecretComparer.FixedTimeEquals, SEC-012) and the rate limiting
    /// ([EnableRateLimiting], SEC-016) are already in place; these tests cover
    /// the strength gate and the success path.
    /// </summary>
    public class IdentityControllerCreateTests
    {
        private const string WeakCode = "1234";
        private const string StrongCode = "9f86d081884c7d659a2feaa0c55ad015"; // 32 hex chars = 128 bits

        [Fact]
        public async Task Create_WeakConfiguredCode_MatchingSubmittedCode_IsRejected()
        {
            var (controller, identityService, _, _) = CreateController(WeakCode);
            var model = CreateModel(WeakCode);

            var result = await controller.Create(model);

            Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey("InviteCode"),
                "a weak configured code must fail closed even when the submitted code matches it");
            Assert.False(identityService.CreateCalled, "no identity may be created when the configured code is weak");
        }

        [Fact]
        public async Task Create_MissingConfiguredCode_AnySubmittedCode_IsRejected()
        {
            var (controller, identityService, _, _) = CreateController(null);
            var model = CreateModel("any-code-at-all-00000000000000000000");

            var result = await controller.Create(model);

            Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey("InviteCode"),
                "a missing configured code must fail closed");
            Assert.False(identityService.CreateCalled, "no identity may be created when no code is configured");
        }

        [Fact]
        public async Task Create_StrongConfiguredCode_WrongSubmittedCode_IsRejected()
        {
            var (controller, identityService, _, _) = CreateController(StrongCode);
            var model = CreateModel("wrong-code-entirely-1111111111111111");

            var result = await controller.Create(model);

            Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey("InviteCode"),
                "a wrong submitted code must be rejected");
            Assert.False(identityService.CreateCalled, "no identity may be created for a wrong code");
        }

        [Fact]
        public async Task Create_StrongConfiguredCode_CorrectCode_CreatesUser()
        {
            var (controller, identityService, localAccountService, loginService) = CreateController(StrongCode);
            var model = CreateModel(StrongCode);

            var result = await controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("UserCreated", viewResult.ViewName);
            Assert.True(identityService.CreateCalled, "the identity must be created");
            Assert.True(localAccountService.CreateCalled, "the local account must be created");
            Assert.True(loginService.LoginCalled, "the new user must be signed in");
        }

        private static (IdentityController, RecordingIdentityService, RecordingLocalAccountService, StubLoginService) CreateController(string? configuredCode)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Rinsen:InvitationCode", configuredCode }
                })
                .Build();

            var identityService = new RecordingIdentityService();
            var localAccountService = new RecordingLocalAccountService();
            var loginService = new StubLoginService();

            var controller = new IdentityController(
                loginService,
                identityService,
                localAccountService,
                new NoOpAuditLog(),
                configuration,
                new StubIdentityAccessor(),
                new NoOpSessionStorage(),
                new IdentityOptions());

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            return (controller, identityService, localAccountService, loginService);
        }

        private static CreateIdentityModel CreateModel(string inviteCode)
        {
            return new CreateIdentityModel
            {
                Email = "new.user@example.com",
                GivenName = "New",
                Surname = "User",
                Password = "correct-horse-battery-staple",
                ConfirmPassword = "correct-horse-battery-staple",
                InviteCode = inviteCode
            };
        }

        private sealed class NoOpAuditLog : AuditLog
        {
            public NoOpAuditLog()
                : base(new AuditLogStorage(new IdentityOptions()))
            {
            }

            public override Task Log(string eventType, string details, string ipAddress)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class RecordingIdentityService : IIdentityService
        {
            private static readonly Guid IdentityId = Guid.NewGuid();

            public bool CreateCalled { get; private set; }

            public Task<Identity> GetIdentityAsync(Guid identityId) => throw new NotImplementedException();
            public Task<Identity> GetIdentityAsync() => throw new NotImplementedException();
            public void UpdateIdentityDetails(string firstName, string lastName, string email, string phoneNumber) => throw new NotImplementedException();

            public Task<CreateIdentityResult> CreateAsync(string firstName, string lastName, string email, string phoneNumber)
            {
                CreateCalled = true;
                return Task.FromResult(CreateIdentityResult.Success(new Identity
                {
                    IdentityId = IdentityId,
                    GivenName = firstName,
                    Surname = lastName,
                    Email = email
                }));
            }
        }

        private sealed class RecordingLocalAccountService : ILocalAccountService
        {
            public bool CreateCalled { get; private set; }

            public Task<CreateLocalAccountResult> CreateAsync(Guid identityId, string loginId, string password)
            {
                CreateCalled = true;
                return Task.FromResult(CreateLocalAccountResult.Success(new LocalAccount
                {
                    IdentityId = identityId,
                    LoginId = loginId
                }));
            }

            public Task<LocalAccount> GetLocalAccountAsync(string loginId, string password) => throw new NotImplementedException();
            public Task<LocalAccount> GetLocalAccountAsync(Guid identityId) => throw new NotImplementedException();
            public Task ChangePasswordAsync(string oldPassword, string newPassword) => throw new NotImplementedException();
            public Task DeleteLocalAccountAsync(string password) => throw new NotImplementedException();
            public Task ValidatePasswordAsync(string password) => throw new NotImplementedException();
            public Task<string> EnableTotp() => throw new NotImplementedException();
        }

        private sealed class StubLoginService : ILoginService
        {
            public bool LoginCalled { get; private set; }

            public Task<LoginResult> LoginAsync(string email, string password, string host, bool rememberMe)
            {
                LoginCalled = true;
                return Task.FromResult(LoginResult.Success(new ClaimsPrincipal(new ClaimsIdentity()), email));
            }

            public Task LogoutAsync() => throw new NotImplementedException();
            public Task StartTotpFlow(string authSessionId) => throw new NotImplementedException();
            public Task<LoginResult> ConfirmTotpCode(string authSessionId, string keyCode, string host, bool rememberMe) => throw new NotImplementedException();
        }

        private sealed class StubIdentityAccessor : IIdentityAccessor
        {
            public ClaimsPrincipal ClaimsPrincipal { get; } = new();
            public Guid IdentityId { get; } = Guid.Empty;
        }

        private sealed class NoOpSessionStorage : ISessionStorage
        {
            public Task CreateAsync(Session session) => Task.CompletedTask;
            public Task DeleteAsync(string sessionId) => Task.CompletedTask;
            public Task<IEnumerable<Session>> GetAsync(Guid identityId, bool includeDeleted = false) => Task.FromResult<IEnumerable<Session>>(new List<Session>());
            public Task<Session?> GetAsync(string sessionId, bool includeDeleted = false) => Task.FromResult<Session?>(null);
            public Task UpdateAsync(Session session) => Task.CompletedTask;
        }

    }
}
