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
    /// SEC-004: Refresh-token reuse detection and family revocation (RFC 9700 §10.5).
    /// </summary>
    public class RefreshTokenReuse
    {
        private const string ClientId = "TestClient";
        private const string RefreshToken = "a-refresh-token-value";

        private static readonly Guid FamilyId = Guid.NewGuid();

        [Fact]
        public async Task NormalRotation_ReturnsAccessToken_AndInheritsFamilyId()
        {
            // Arrange
            var grant = CreateGrant(familyId: FamilyId, resolved: null);
            var handler = CreateHandler(grant, out var grantAccessor);

            // Act
            var result = await handler.GetTokenAsync(CreateTokenModel(), CreateClient(), "https://localhost");

            // Assert
            Assert.False(result.IsError);
            Assert.Null(result.ErrorResponse);
            Assert.NotNull(result.AccessTokenResponse);

            // The rotated grant inherits the source grant's family id
            Assert.NotNull(grantAccessor.SavedGrant);
            Assert.Equal(FamilyId, grantAccessor.SavedGrant!.FamilyId);
        }

        [Fact]
        public async Task NormalRotationOfLegacyGrant_GeneratesNewFamilyId()
        {
            // Arrange: a grant that predates family ids (FamilyId == Guid.Empty)
            var grant = CreateGrant(familyId: Guid.Empty, resolved: null);
            var handler = CreateHandler(grant, out var grantAccessor);

            // Act
            var result = await handler.GetTokenAsync(CreateTokenModel(), CreateClient(), "https://localhost");

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.AccessTokenResponse);

            // The rotated grant gets a fresh family id instead of the empty one
            Assert.NotNull(grantAccessor.SavedGrant);
            Assert.NotEqual(Guid.Empty, grantAccessor.SavedGrant!.FamilyId);
        }

        [Fact]
        public async Task ReplayOfResolvedToken_ReturnsInvalidGrant_AndRevokesFamily()
        {
            // Arrange: the token was already consumed (rotated) and is now replayed
            var grant = CreateGrant(familyId: FamilyId, resolved: DateTimeOffset.UtcNow);
            var handler = CreateHandler(grant, out var grantAccessor);

            // Act
            var result = await handler.GetTokenAsync(CreateTokenModel(), CreateClient(), "https://localhost");

            // Assert
            Assert.True(result.IsError);
            Assert.NotNull(result.ErrorResponse);
            Assert.Equal(ErrorResponses.InvalidGrant, result.ErrorResponse!.Error);
            Assert.Null(result.AccessTokenResponse);

            // The replayed token is not rotated
            Assert.Null(grantAccessor.SavedGrant);

            // The entire token family is revoked
            Assert.Contains(FamilyId, grantAccessor.RevokedFamilyIds);
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

        private static RefreshTokenGrant CreateGrant(Guid familyId, DateTimeOffset? resolved)
        {
            return new RefreshTokenGrant
            {
                ClientId = ClientId,
                SubjectId = "test-user",
                RefreshToken = RefreshToken,
                Scope = "openid",
                Created = DateTimeOffset.UtcNow.AddHours(-2),
                Expires = DateTimeOffset.UtcNow.AddHours(1),
                FamilyId = familyId,
                Resolved = resolved,
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
