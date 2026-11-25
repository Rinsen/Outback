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
    /// SEC-005: A device code must be single-use (RFC 8628 §3.2).
    /// </summary>
    public class DeviceCodeReuse
    {
        private const string ClientId = "TestClient";
        private const string DeviceCode = "a-device-code-value";

        [Fact]
        public async Task FirstRedemption_ReturnsAccessToken_AndResolvesGrant()
        {
            // Arrange
            var grant = CreateGrant(resolved: null);
            var handler = CreateHandler(grant, out var grantAccessor);

            // Act
            var result = await handler.GetTokenAsync(CreateTokenModel(), CreateClient(), "https://localhost");

            // Assert
            Assert.False(result.IsError);
            Assert.Null(result.ErrorResponse);
            Assert.NotNull(result.AccessTokenResponse);

            // The redeemed device code is marked resolved
            Assert.Contains(DeviceCode, grantAccessor.ResolvedDeviceCodes);
        }

        [Fact]
        public async Task SecondRedemptionOfResolvedGrant_ReturnsInvalidGrant()
        {
            // Arrange: the device code was already redeemed and is now replayed
            var grant = CreateGrant(resolved: DateTimeOffset.UtcNow);
            var handler = CreateHandler(grant, out var grantAccessor);

            // Act
            var result = await handler.GetTokenAsync(CreateTokenModel(), CreateClient(), "https://localhost");

            // Assert
            Assert.True(result.IsError);
            Assert.NotNull(result.ErrorResponse);
            Assert.Equal(ErrorResponses.InvalidGrant, result.ErrorResponse!.Error);
            Assert.Null(result.AccessTokenResponse);

            // A replayed device code is not resolved again
            Assert.Empty(grantAccessor.ResolvedDeviceCodes);
        }

        private static DeviceCodeGrantTypeHandler CreateHandler(DeviceCodeGrant grant, out FakeGrantAccessor grantAccessor)
        {
            grantAccessor = new FakeGrantAccessor(null, deviceGrant: grant);

            return new DeviceCodeGrantTypeHandler(
                grantAccessor,
                new FakeTokenService(),
                new RandomStringGenerator(),
                NullLogger<DeviceCodeGrantTypeHandler>.Instance);
        }

        private static DeviceCodeGrant CreateGrant(DateTimeOffset? resolved)
        {
            return new DeviceCodeGrant
            {
                ClientId = ClientId,
                SubjectId = "test-user",
                DeviceCode = DeviceCode,
                UserCode = "ABCD-EFGH",
                Scope = "openid",
                UserCodeExpiration = DateTimeOffset.UtcNow.AddHours(1),
                Interval = 5,
                Resolved = resolved,
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
