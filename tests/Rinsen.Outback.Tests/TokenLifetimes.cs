using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Clients;
using Rinsen.Outback.JwtTokens;
using AuthorizationCodeGrantModel = Rinsen.Outback.Grants.AuthorizationCodeGrant;
using Rinsen.Outback.Scopes;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// SEC-006: The access-token exp must use client.AccessTokenLifetime,
    /// the ID-token exp must use client.IdentityTokenLifetime, and
    /// expires_in must match the issued access-token exp.
    /// </summary>
    public class TokenLifetimes
    {
        private const string ClientId = "TestClient";
        private const int AccessTokenLifetimeSeconds = 100;
        private const int IdentityTokenLifetimeSeconds = 300;

        [Fact]
        public async Task AuthorizationCodeFlow_AccessToken_ExpiresAfterAccessTokenLifetime()
        {
            var tokenService = CreateTokenService();

            var response = await tokenService.CreateTokenResponseAsync(CreateClient(), CreateGrant(), "a-refresh-token", "https://localhost");

            // expires_in matches the issued access-token exp
            Assert.Equal(AccessTokenLifetimeSeconds, response.ExpiresIn);
            AssertLifetime(response.AccessToken, AccessTokenLifetimeSeconds);
        }

        [Fact]
        public async Task AuthorizationCodeFlow_IdentityToken_ExpiresAfterIdentityTokenLifetime()
        {
            var tokenService = CreateTokenService();

            var response = await tokenService.CreateTokenResponseAsync(CreateClient(), CreateGrant(), "a-refresh-token", "https://localhost");

            Assert.NotNull(response.IdentityToken);
            AssertLifetime(response.IdentityToken, IdentityTokenLifetimeSeconds);
        }

        [Fact]
        public async Task ClientCredentials_AccessToken_ExpiresAfterAccessTokenLifetime()
        {
            var tokenService = CreateTokenService();

            var response = await tokenService.CreateTokenResponseAsync(CreateClient(), "https://localhost");

            // expires_in matches the issued access-token exp
            Assert.Equal(AccessTokenLifetimeSeconds, response.ExpiresIn);
            AssertLifetime(response.AccessToken, AccessTokenLifetimeSeconds);
        }

        /// <summary>
        /// Asserts that exp - iat equals the expected lifetime, allowing up to one extra
        /// second for truncation of the two timestamps to whole seconds.
        /// </summary>
        private static void AssertLifetime(string token, int expectedLifetimeSeconds)
        {
            var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
            var issuedAt = long.Parse(jwt.Claims.Single(c => c.Type == "iat").Value);
            var expires = long.Parse(jwt.Claims.Single(c => c.Type == "exp").Value);

            Assert.InRange(expires - issuedAt, expectedLifetimeSeconds, expectedLifetimeSeconds + 1);
        }

        private static TokenService CreateTokenService()
        {
            return new TokenService(new FakeTokenSigningAccessor(), new FakeUserInfoAccessor(), new FakeScopeAccessor());
        }

        private static Client CreateClient()
        {
            return new Client
            {
                ClientId = ClientId,
                AccessTokenLifetime = AccessTokenLifetimeSeconds,
                IdentityTokenLifetime = IdentityTokenLifetimeSeconds,
                IssueRefreshToken = true,
                IssueIdentityToken = true,
                Scopes = new List<string> { "openid" },
            };
        }

        private static AuthorizationCodeGrantModel CreateGrant()
        {
            return new AuthorizationCodeGrantModel
            {
                ClientId = ClientId,
                SubjectId = "test-user",
                Code = "a-code",
                Scope = "openid",
                Nonce = "a-nonce",
            };
        }

        private class FakeTokenSigningAccessor : ITokenSigningAccessor
        {
            public Task<SecurityKeyWithAlgorithm> GetSigningSecurityKey()
            {
                var key = new ECDsaSecurityKey(ECDsa.Create(ECCurve.NamedCurves.nistP256));

                return Task.FromResult(new SecurityKeyWithAlgorithm(key, SecurityAlgorithms.EcdsaSha256));
            }
        }

        private class FakeUserInfoAccessor : IUserInfoAccessor
        {
            public Task<Dictionary<string, string>> GetUserInfoClaims(string subjectId, IEnumerable<string> scopes)
            {
                return Task.FromResult(new Dictionary<string, string>());
            }
        }

        private class FakeScopeAccessor : IScopeAccessor
        {
            public Task<List<Scope>> GetScopesAsync()
            {
                return Task.FromResult(CreateScopes());
            }

            public Task<List<Scope>> GetScopesAsync(IReadOnlyList<string> scopes)
            {
                return Task.FromResult(CreateScopes());
            }

            private static List<Scope> CreateScopes()
            {
                return new List<Scope>
                {
                    new Scope { ScopeName = "openid", Audience = "https://api.example.com" },
                };
            }
        }
    }
}
