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
    /// SEC-001: A refresh token must be rejected once its grant has expired.
    /// </summary>
    public class RefreshTokenExpiration
    {
        private const string ClientId = "TestClient";
        private const string RefreshToken = "a-refresh-token-value";

        [Fact]
        public async Task ExpiredRefreshToken_ReturnsInvalidGrant()
        {
            // Arrange
            var grant = CreateGrant(expires: DateTimeOffset.UtcNow.AddSeconds(-1));
            var handler = CreateHandler(grant, out var grantAccessor);

            // Act
            var result = await handler.GetTokenAsync(CreateTokenModel(), CreateClient(), "https://localhost");

            // Assert
            Assert.True(result.IsError);
            Assert.NotNull(result.ErrorResponse);
            Assert.Equal(ErrorResponses.InvalidGrant, result.ErrorResponse!.Error);
            Assert.Null(result.ErrorResponse.ErrorDescription);
            Assert.Null(result.AccessTokenResponse);

            // An expired token must not be rotated
            Assert.Null(grantAccessor.SavedGrant);
        }

        [Fact]
        public async Task UnexpiredRefreshToken_ReturnsAccessToken()
        {
            // Arrange
            var grant = CreateGrant(expires: DateTimeOffset.UtcNow.AddHours(1));
            var handler = CreateHandler(grant, out var grantAccessor);

            // Act
            var result = await handler.GetTokenAsync(CreateTokenModel(), CreateClient(), "https://localhost");

            // Assert
            Assert.False(result.IsError);
            Assert.Null(result.ErrorResponse);
            Assert.NotNull(result.AccessTokenResponse);
            Assert.Equal("TestAccessToken", result.AccessTokenResponse!.AccessToken);

            // A valid token is rotated: a new grant is saved for the same client
            Assert.NotNull(grantAccessor.SavedGrant);
            Assert.Equal(ClientId, grantAccessor.SavedGrant!.ClientId);
        }

        private static RefreshTokenGrantTypeHandler CreateHandler(RefreshTokenGrant grant, out FakeGrantAccessor grantAccessor)
        {
            grantAccessor = new FakeGrantAccessor(grant);

            return new RefreshTokenGrantTypeHandler(
                grantAccessor,
                new FakeTokenService(),
                new RandomStringGenerator(),
                NullLogger<RefreshTokenGrantTypeHandler>.Instance);
        }

        private static RefreshTokenGrant CreateGrant(DateTimeOffset expires)
        {
            return new RefreshTokenGrant
            {
                ClientId = ClientId,
                SubjectId = "test-user",
                RefreshToken = RefreshToken,
                Scope = "openid",
                Created = DateTimeOffset.UtcNow.AddHours(-2),
                Expires = expires,
            };
        }

        private static Client CreateClient()
        {
            return new Client
            {
                ClientId = ClientId,
                RefreshTokenLifetime = 3600,
            };
        }

        private static TokenModel CreateTokenModel()
        {
            return new TokenModel
            {
                GrantType = "refresh_token",
                RefreshToken = RefreshToken,
            };
        }
    }
}
