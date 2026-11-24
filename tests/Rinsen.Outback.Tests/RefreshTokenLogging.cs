using System;
using System.Threading.Tasks;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Grants;
using Rinsen.Outback.Helpers;
using Rinsen.Outback.Models;
using Rinsen.Outback.Tests.Helpers;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// SEC-002: A failed refresh-token attempt must not leak the raw token into the log,
    /// neither in the log message nor in the exception that is logged with it.
    /// </summary>
    public class RefreshTokenLogging
    {
        private const string ClientId = "TestClient";
        private const string RefreshToken = "super-secret-refresh-token-value";

        [Fact]
        public async Task FailedRefreshAttempt_LogContainsNoToken()
        {
            // Arrange: the grant lookup fails, as it does for an unknown token
            var logger = new CapturingLogger<RefreshTokenGrantTypeHandler>();
            var grantAccessor = new FakeGrantAccessor(
                grant: null,
                getRefreshTokenGrantException: new Exception("Refresh token not found"));
            var handler = new RefreshTokenGrantTypeHandler(
                grantAccessor,
                new FakeTokenService(),
                new RandomStringGenerator(),
                logger);

            // Act
            var result = await handler.GetTokenAsync(CreateTokenModel(), CreateClient(), "https://localhost");

            // Assert: the request still fails with invalid_grant ...
            Assert.True(result.IsError);
            Assert.NotNull(result.ErrorResponse);
            Assert.Equal(ErrorResponses.InvalidGrant, result.ErrorResponse!.Error);
            Assert.Null(result.AccessTokenResponse);

            // ... and the raw token never appears in the log (message or exception)
            Assert.NotEmpty(logger.Entries);
            foreach (var entry in logger.Entries)
            {
                Assert.DoesNotContain(RefreshToken, entry);
            }
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
