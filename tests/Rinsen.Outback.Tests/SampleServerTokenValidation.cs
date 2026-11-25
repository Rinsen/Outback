using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Rinsen.Outback.Accessors;
using SampleServer;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// SEC-011: SampleServer must validate inbound JWT bearer tokens with a real
    /// Authority/Audience, an ES256-only algorithm whitelist, and the issuer's own
    /// signing key. A token signed by a foreign issuer's key must be rejected.
    /// </summary>
    public class SampleServerTokenValidation
    {
        private const string ValidIssuer = "http://localhost";
        private const string ValidAudience = "OutbackAS";
        private const string ForeignIssuer = "https://foreign.example.com";

        [Fact]
        public async Task TokenSignedByForeignIssuer_IsRejected()
        {
            var application = new WebApplicationFactory<Program>();
            var client = application.CreateClient();

            // A token signed with a key that is NOT the SampleServer's signing key.
            var foreignKey = new ECDsaSecurityKey(ECDsa.Create(ECCurve.NamedCurves.nistP256));
            var foreignToken = CreateToken(foreignKey, ForeignIssuer, ValidAudience);

            var response = await client.SendAsync(CreateUserInfoRequest(foreignToken));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TokenSignedBySampleServer_IsAccepted()
        {
            var application = new WebApplicationFactory<Program>();
            var client = application.CreateClient();

            // The SampleServer's actual signing key, resolved from its DI container.
            var signingAccessor = application.Services.GetRequiredService<ITokenSigningAccessor>();
            var key = await signingAccessor.GetSigningSecurityKey();

            var validToken = CreateToken(key.SecurityKey, ValidIssuer, ValidAudience);

            var response = await client.SendAsync(CreateUserInfoRequest(validToken));

            // The token must pass authentication (not 401). The sample's UserInfoAccessor
            // is a stub that throws, so a non-401 status proves the token was accepted.
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TokenWithWrongAudience_IsRejected()
        {
            var application = new WebApplicationFactory<Program>();
            var client = application.CreateClient();

            var signingAccessor = application.Services.GetRequiredService<ITokenSigningAccessor>();
            var key = await signingAccessor.GetSigningSecurityKey();

            // Signed with the correct key, but for a different audience.
            var wrongAudienceToken = CreateToken(key.SecurityKey, ValidIssuer, "SomeOtherAudience");

            var response = await client.SendAsync(CreateUserInfoRequest(wrongAudienceToken));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private static HttpRequestMessage CreateUserInfoRequest(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "oauth/userinfo");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        private static string CreateToken(SecurityKey key, string issuer, string audience)
        {
            var handler = new JsonWebTokenHandler();
            var now = DateTime.UtcNow;

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Audience = audience,
                IssuedAt = now,
                Expires = now.AddMinutes(5),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256),
            };

            descriptor.Claims = new System.Collections.Generic.Dictionary<string, object>
            {
                { "sub", "test-user" },
                { "scope", "openid" },
            };

            return handler.CreateToken(descriptor);
        }
    }
}
