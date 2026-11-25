using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rinsen.IdentityProvider;
using Rinsen.IdentityProvider.Outback;
using Rinsen.IdentityProvider.Outback.Entities;
using Rinsen.Outback.App.Controllers;
using Rinsen.Outback.App.Models;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// FUNC-003: the device consent POST in the production app (EF storage).
    /// Authorizing must bind the grant to the consenting user (SubjectId), which is
    /// what DeviceCodeGrantTypeHandler checks before issuing a token; rejecting must
    /// set AccessIsRejected so the token endpoint answers access_denied.
    /// </summary>
    public class DeviceConsentTests
    {
        private static readonly Guid IdentityId = Guid.NewGuid();
        private const string UserCode = "ABCD-EFGH";

        [Fact]
        public async Task Consent_Authorize_BindsGrantToConsentingUser()
        {
            // Arrange
            var (controller, options) = await CreateControllerAsync(
                subjectId: Guid.Empty, accessIsRejected: false, resolved: null, expired: false);

            // Act
            var result = await controller.Consent(CreateModel(authorize: true), CancellationToken.None);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("ConsentGranted", viewResult.ViewName);

            await using var context = new OutbackDbContext(options);
            var stored = await context.DeviceAuthorizationGrants.SingleAsync(m => m.UserCode == UserCode);
            Assert.Equal(IdentityId, stored.SubjectId);
            Assert.NotNull(stored.Accepted);
            Assert.False(stored.AccessIsRejected);
        }

        [Fact]
        public async Task Consent_Reject_MarksGrantRejected()
        {
            // Arrange
            var (controller, options) = await CreateControllerAsync(
                subjectId: Guid.Empty, accessIsRejected: false, resolved: null, expired: false);

            // Act
            var result = await controller.Consent(CreateModel(authorize: false), CancellationToken.None);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("ConsentDenied", viewResult.ViewName);

            await using var context = new OutbackDbContext(options);
            var stored = await context.DeviceAuthorizationGrants.SingleAsync(m => m.UserCode == UserCode);
            Assert.Equal(Guid.Empty, stored.SubjectId);
            Assert.Null(stored.Accepted);
            Assert.True(stored.AccessIsRejected);
        }

        [Fact]
        public async Task Consent_UnknownUserCode_ChangesNothing()
        {
            // Arrange
            var (controller, options) = await CreateControllerAsync(
                subjectId: Guid.Empty, accessIsRejected: false, resolved: null, expired: false);

            // Act
            var result = await controller.Consent(new DeviceConsentViewModel { UserCode = "ZZZZ-ZZZZ" }, CancellationToken.None);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);

            await using var context = new OutbackDbContext(options);
            var stored = await context.DeviceAuthorizationGrants.SingleAsync(m => m.UserCode == UserCode);
            Assert.Equal(Guid.Empty, stored.SubjectId);
            Assert.False(stored.AccessIsRejected);
        }

        [Fact]
        public async Task Consent_ExpiredGrant_CannotBeConsented()
        {
            // Arrange
            var (controller, options) = await CreateControllerAsync(
                subjectId: Guid.Empty, accessIsRejected: false, resolved: null, expired: true);

            // Act
            var result = await controller.Consent(CreateModel(authorize: true), CancellationToken.None);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("UserCodeExpired", viewResult.ViewName);

            await using var context = new OutbackDbContext(options);
            var stored = await context.DeviceAuthorizationGrants.SingleAsync(m => m.UserCode == UserCode);
            Assert.Equal(Guid.Empty, stored.SubjectId);
            Assert.False(stored.AccessIsRejected);
        }

        [Fact]
        public async Task Consent_AlreadyConsented_DoesNotOverwriteSubject()
        {
            // Arrange: the grant was already consented by another user
            var firstSubject = Guid.NewGuid();
            var (controller, options) = await CreateControllerAsync(
                subjectId: firstSubject, accessIsRejected: false, resolved: null, expired: false);

            // Act
            var result = await controller.Consent(CreateModel(authorize: true), CancellationToken.None);

            // Assert: the original subject is kept
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("ConsentGranted", viewResult.ViewName);

            await using var context = new OutbackDbContext(options);
            var stored = await context.DeviceAuthorizationGrants.SingleAsync(m => m.UserCode == UserCode);
            Assert.Equal(firstSubject, stored.SubjectId);
        }

        [Fact]
        public async Task Consent_AlreadyRejected_StaysRejected()
        {
            // Arrange
            var (controller, options) = await CreateControllerAsync(
                subjectId: Guid.Empty, accessIsRejected: true, resolved: null, expired: false);

            // Act
            var result = await controller.Consent(CreateModel(authorize: true), CancellationToken.None);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("ConsentDenied", viewResult.ViewName);

            await using var context = new OutbackDbContext(options);
            var stored = await context.DeviceAuthorizationGrants.SingleAsync(m => m.UserCode == UserCode);
            Assert.Equal(Guid.Empty, stored.SubjectId);
            Assert.True(stored.AccessIsRejected);
        }

        [Fact]
        public async Task Consent_ResolvedGrant_CannotBeConsented()
        {
            // Arrange: the device code was already redeemed
            var (controller, options) = await CreateControllerAsync(
                subjectId: IdentityId, accessIsRejected: false, resolved: DateTimeOffset.UtcNow, expired: false);

            // Act
            var result = await controller.Consent(CreateModel(authorize: true), CancellationToken.None);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("UserCodeExpired", viewResult.ViewName);
        }

        private static DeviceConsentViewModel CreateModel(bool authorize)
        {
            return new DeviceConsentViewModel
            {
                UserCode = UserCode,
                Authorize = authorize,
            };
        }

        private static async Task<(DeviceController, DbContextOptions<OutbackDbContext>)> CreateControllerAsync(
            Guid subjectId, bool accessIsRejected, DateTimeOffset? resolved, bool expired)
        {
            var options = new DbContextOptionsBuilder<OutbackDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using (var context = new OutbackDbContext(options))
            {
                var client = new OutbackClient { ClientId = "TestClient" };
                context.Clients.Add(client);
                await context.SaveChangesAsync();

                context.DeviceAuthorizationGrants.Add(new OutbackDeviceAuthorizationGrant
                {
                    ClientId = client.Id,
                    SubjectId = subjectId,
                    AccessIsRejected = accessIsRejected,
                    DeviceCode = "device-code-1",
                    UserCode = UserCode,
                    Scope = "openid profile",
                    Created = DateTimeOffset.UtcNow,
                    UserCodeExpiration = expired ? DateTimeOffset.UtcNow.AddHours(-1) : DateTimeOffset.UtcNow.AddHours(1),
                    Interval = 5,
                    Resolved = resolved,
                });

                await context.SaveChangesAsync();
            }

            var controller = new DeviceController(new OutbackDbContext(options), new FakeIdentityAccessor(IdentityId));

            return (controller, options);
        }

        private sealed class FakeIdentityAccessor : IIdentityAccessor
        {
            public FakeIdentityAccessor(Guid identityId)
            {
                IdentityId = identityId;
            }

            public ClaimsPrincipal ClaimsPrincipal => throw new NotSupportedException();

            public Guid IdentityId { get; }
        }
    }
}
