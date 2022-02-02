using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Rinsen.Outback.Tests.Helpers
{
    internal class JwtValidationHelper
    {
        public static async Task<TokenValidationResult> ValidateToken(HttpClient client, string token, List<string> validAudiences, string validIssuer)
        {
            var key = await client.GetJsonWebKey();
            var securityKey = new JsonWebKey()
            {
                KeyId = key.KeyId,
                Kty = key.KeyType,
                Alg = key.SigningAlgorithm,
                X = key.X,
                Y = key.Y,
                Crv = key.Curve,
            };

            IdentityModelEventSource.ShowPII = true;
            var tokenHandler = new JsonWebTokenHandler();
            var tokenValidationResult = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidAudiences = validAudiences,
                ValidIssuer = validIssuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidateAudience = true,
                ClockSkew = TimeSpan.Zero
            });
            
            return tokenValidationResult;
        }

        public static async Task<TokenValidationResult> ValidateToken(HttpClient client, string token, string validAudience, string validIssuer)
        {
            var key = await client.GetJsonWebKey();
            var securityKey = new JsonWebKey()
            {
                KeyId = key.KeyId,
                Kty = key.KeyType,
                Alg = key.SigningAlgorithm,
                X = key.X,
                Y = key.Y,
                Crv = key.Curve,
            };

            IdentityModelEventSource.ShowPII = true;
            var tokenHandler = new JsonWebTokenHandler();
            var tokenValidationResult = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidAudience = validAudience,
                ValidIssuer = validIssuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidateAudience = true,
                ClockSkew = TimeSpan.Zero
            });

            return tokenValidationResult;
        }
    }
}
