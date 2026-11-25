using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rinsen.IdentityProvider;
using Rinsen.IdentityProvider.AuditLogging;
using Rinsen.IdentityProvider.LocalAccounts;
using Rinsen.Outback.App.Controllers;
using Rinsen.Outback.App.Models;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// SEC-021: after a successful login, an external returnUrl must never be
    /// honored (open redirect). It is validated with Url.IsLocalUrl in every
    /// environment and anything external falls back to the site root.
    /// </summary>
    public class ReturnUrlTests
    {
        [Fact]
        public async Task Login_ExternalReturnUrl_RedirectsToSiteRoot()
        {
            var (controller, loginService) = CreateController();
            var model = CreateLoginModel("https://evil.example.com/phish");

            var result = await controller.Login(model);

            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal("~/", redirect.Url);
            Assert.True(loginService.LoginCalled, "the login itself must succeed");
        }

        [Fact]
        public async Task Login_ProtocolRelativeReturnUrl_RedirectsToSiteRoot()
        {
            var (controller, loginService) = CreateController();
            var model = CreateLoginModel("//evil.example.com/phish");

            var result = await controller.Login(model);

            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal("~/", redirect.Url);
            Assert.True(loginService.LoginCalled, "the login itself must succeed");
        }

        [Fact]
        public async Task Login_EmptyReturnUrl_RedirectsToSiteRoot()
        {
            var (controller, loginService) = CreateController();
            var model = CreateLoginModel(string.Empty);

            var result = await controller.Login(model);

            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal("~/", redirect.Url);
            Assert.True(loginService.LoginCalled, "the login itself must succeed");
        }

        [Fact]
        public async Task Login_LocalReturnUrl_IsHonored()
        {
            var (controller, loginService) = CreateController();
            var model = CreateLoginModel("/dashboard");

            var result = await controller.Login(model);

            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/dashboard", redirect.Url);
            Assert.True(loginService.LoginCalled, "the login itself must succeed");
        }

        private static (IdentityController, StubLoginService) CreateController()
        {
            var configuration = new ConfigurationBuilder().Build();

            var loginService = new StubLoginService();

            var controller = new IdentityController(
                loginService,
                new StubIdentityService(),
                new StubLocalAccountService(),
                new NoOpAuditLog(),
                configuration,
                new StubIdentityAccessor(),
                new NoOpSessionStorage(),
                new IdentityOptions());

            // Url.IsLocalUrl is resolved through the controller's UrlHelper,
            // which requires IUrlHelperFactory and an
            // IActionDescriptorCollectionProvider in the request services.
            var services = new ServiceCollection();
            services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
            services.AddSingleton<IActionDescriptorCollectionProvider, TestActionDescriptorCollectionProvider>();

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services.BuildServiceProvider();

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData()
            };

            return (controller, loginService);
        }

        private static LoginModel CreateLoginModel(string returnUrl)
        {
            return new LoginModel
            {
                Email = "user@example.com",
                Password = "correct-horse-battery-staple",
                ReturnUrl = returnUrl
            };
        }

        private sealed class TestActionDescriptorCollectionProvider : IActionDescriptorCollectionProvider
        {
            public ActionDescriptorCollection ActionDescriptors { get; } = new ActionDescriptorCollection(Array.Empty<ActionDescriptor>(), 0);
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

        private sealed class StubIdentityService : IIdentityService
        {
            public Task<Identity> GetIdentityAsync(Guid identityId) => throw new NotImplementedException();
            public Task<Identity> GetIdentityAsync() => throw new NotImplementedException();
            public void UpdateIdentityDetails(string firstName, string lastName, string email, string phoneNumber) => throw new NotImplementedException();
            public Task<CreateIdentityResult> CreateAsync(string firstName, string lastName, string email, string phoneNumber) => throw new NotImplementedException();
        }

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
