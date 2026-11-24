using System.Threading.Tasks;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Configuration;
using Rinsen.Outback.Grants;
using Rinsen.Outback.Helpers;
using Rinsen.Outback.Models;
using Rinsen.Outback.Tests.Helpers;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// SEC-003: A malformed code verifier (the PKCE secret) must not leak into the log.
    /// </summary>
    public class CodeVerifierLogging
    {
        private const string ClientId = "TestClient";
        private const string Code = "test-authorization-code";
        private const string CodeVerifier = "super-secret-code-verifier-value";

        [Fact]
        public async Task InvalidCodeVerifier_ReturnsInvalidRequest_AndLogContainsNoVerifier()
        {
            // Arrange: a verifier outside the 43-128 length range
            var logger = new CapturingLogger<AuthorizationCodeGrantTypeHandler>();
            var handler = new AuthorizationCodeGrantTypeHandler(
                new CodeGrantActiveConfigurationAccessor(),
                new FakeTokenService(),
                new FakeGrantAccessor(grant: null),
                new RandomStringGenerator(),
                logger);

            // Act
            var result = await handler.GetTokenAsync(CreateTokenModel(), CreateClient(), "https://localhost");

            // Assert: the request fails with invalid_request ...
            Assert.True(result.IsError);
            Assert.NotNull(result.ErrorResponse);
            Assert.Equal(ErrorResponses.InvalidRequest, result.ErrorResponse!.Error);
            Assert.Null(result.AccessTokenResponse);

            // ... and the raw code verifier never appears in the log
            Assert.NotEmpty(logger.Entries);
            foreach (var entry in logger.Entries)
            {
                Assert.DoesNotContain(CodeVerifier, entry);
            }
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
                GrantType = "authorization_code",
                Code = Code,
                CodeVerifier = CodeVerifier,
            };
        }

        private sealed class CodeGrantActiveConfigurationAccessor : IOutbackConfigurationAccessor
        {
            public Task<bool> IsDeviceAuthorizationGrantActiveAsync() => Task.FromResult(false);

            public Task<bool> IsClientCredentialsGrantActiveAsync() => Task.FromResult(false);

            public Task<bool> IsRefreshTokenGrantActiveAsync() => Task.FromResult(false);

            public Task<bool> IsCodeGrantActiveAsync() => Task.FromResult(true);

            public Task<bool> IsClientSecretBasicAuthenticationActiveAsync() => Task.FromResult(false);

            public Task<bool> IsClientSecretPostAuthenticationActiveAsync() => Task.FromResult(false);
        }
    }
}
