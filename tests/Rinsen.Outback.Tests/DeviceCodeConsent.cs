using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Grants;
using Rinsen.Outback.Helpers;
using Rinsen.Outback.Models;
using Rinsen.Outback.Tests.Helpers;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// FUNC-003: the token endpoint's reaction to the consent state that the
    /// DeviceController consent POST writes — pending (SubjectId empty) vs.
    /// rejected (AccessIsRejected) vs. consented (SubjectId set).
    /// </summary>
    public class DeviceCodeConsent
    {
        private const string ClientId = "TestClient";
        private const string DeviceCode = "a-device-code-value";

        [Fact]
        public async Task PendingGrant_ReturnsAuthorizationPending()
        {
            // Arrange: the user has not consented yet (SubjectId is empty)
            var grant = CreateGrant(subjectId: null, accessIsRejected: false);
            var handler = CreateHandler(grant);

            // Act
            var result = await handler.GetTokenAsync(CreateTokenModel(), CreateClient(), "https://localhost");

            // Assert
            Assert.True(result.IsError);
            Assert.NotNull(result.ErrorResponse);
            Assert.Equal(ErrorResponses.AuthorizationPending, result.ErrorResponse!.Error);
            Assert.Null(result.AccessTokenResponse);
        }

        [Fact]
        public async Task RejectedGrant_ReturnsAccessDenied()
        {
            // Arrange: the user explicitly rejected the device
            var grant = CreateGrant(subjectId: null, accessIsRejected: true);
            var handler = CreateHandler(grant);

            // Act
            var result = await handler.GetTokenAsync(CreateTokenModel(), CreateClient(), "https://localhost");

            // Assert
            Assert.True(result.IsError);
            Assert.NotNull(result.ErrorResponse);
            Assert.Equal(ErrorResponses.AccessDenied, result.ErrorResponse!.Error);
            Assert.Null(result.AccessTokenResponse);
        }

        private static DeviceCodeGrantTypeHandler CreateHandler(DeviceCodeGrant grant)
        {
            return new DeviceCodeGrantTypeHandler(
                new FakeGrantAccessor(null, deviceGrant: grant),
                new FakeTokenService(),
                new RandomStringGenerator(),
                NullLogger<DeviceCodeGrantTypeHandler>.Instance);
        }

        private static DeviceCodeGrant CreateGrant(string? subjectId, bool accessIsRejected)
        {
            return new DeviceCodeGrant
            {
                ClientId = ClientId,
                SubjectId = subjectId,
                AccessIsRejected = accessIsRejected,
                DeviceCode = DeviceCode,
                UserCode = "ABCD-EFGH",
                Scope = "openid",
                UserCodeExpiration = DateTimeOffset.UtcNow.AddHours(1),
                Interval = 5,
                Resolved = null,
            };
        }

        private static Client CreateClient()
        {
            return new Client
            {
                ClientId = ClientId,
            };
        }

        private static TokenModel CreateTokenModel()
        {
            return new TokenModel
            {
                GrantType = "urn:ietf:params:oauth:grant-type:device_code",
                DeviceCode = DeviceCode,
            };
        }
    }
}
