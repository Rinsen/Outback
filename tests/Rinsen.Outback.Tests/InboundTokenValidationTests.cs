using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Rinsen.Outback.JwtTokens;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// SEC-010: The inbound JWT bearer validation must use explicit
    /// <see cref="TokenValidationParameters"/> — all Validate* flags on, an
    /// algorithm whitelist of ES256 only, and a 5 minute clock skew. A token
    /// signed with a non-ES256 key must be rejected.
    /// </summary>
    public class InboundTokenValidationTests
    {
        private const string Issuer = "https://issuer.example";
        private const string Audience = "outback-client";

        [Fact]
        public void SetsExplicitValidationFlags_AlgorithmWhitelist_AndClockSkew()
        {
            var parameters = InboundTokenValidation.CreateTokenValidationParameters();

            Assert.True(parameters.ValidateIssuer);
            Assert.True(parameters.ValidateAudience);
            Assert.True(parameters.ValidateLifetime);
            Assert.True(parameters.ValidateIssuerSigningKey);
            Assert.Equal(new[] { SecurityAlgorithms.EcdsaSha256 }, parameters.ValidAlgorithms);
            Assert.Equal(TimeSpan.FromMinutes(5), parameters.ClockSkew);
        }

        [Fact]
        public async Task TokenSignedWithNonEs256Key_IsRejected()
        {
            // HMAC (HS256) key — a valid signature, but not an ES256 key
            var symmetricKey = new SymmetricSecurityKey(GenerateKeyBytes());
            var token = CreateToken(SecurityAlgorithms.HmacSha256, symmetricKey);

            var result = await Validate(token, symmetricKey);

            Assert.False(result.IsValid);
            Assert.NotNull(result.Exception);
        }

        [Fact]
        public async Task TokenSignedWithEs256Key_IsAccepted()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var ecdsaKey = new ECDsaSecurityKey(ecdsa);
            var token = CreateToken(SecurityAlgorithms.EcdsaSha256, ecdsaKey);

            var result = await Validate(token, ecdsaKey);

            Assert.True(result.IsValid);
            Assert.Null(result.Exception);
        }

        private static Task<TokenValidationResult> Validate(string token, SecurityKey key)
        {
            var parameters = InboundTokenValidation.CreateTokenValidationParameters();
            parameters.ValidIssuer = Issuer;
            parameters.ValidAudience = Audience;
            parameters.IssuerSigningKey = key;

            return new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);
        }

        private static byte[] GenerateKeyBytes()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return bytes;
        }

        private static string CreateToken(string algorithm, SecurityKey key)
        {
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                Audience = Audience,
                IssuedAt = DateTime.UtcNow.AddMinutes(-5),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(key, algorithm)
            };
            descriptor.Claims = new Dictionary<string, object> { { "sub", "test-subject" } };

            return new JsonWebTokenHandler().CreateToken(descriptor);
        }
    }
}
