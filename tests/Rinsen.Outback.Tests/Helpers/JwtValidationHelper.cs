﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Rinsen.Outback.Tests.Helpers
{
    internal class JwtValidationHelper
    {
        public static async Task<JwtSecurityToken> ValidateToken(HttpClient client, string token, List<string> validAudiences, string validIssuer)
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
            //JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
            var tokenHandler = new JsonWebTokenHandler();
https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/blob/dev/src/Microsoft.IdentityModel.JsonWebTokens/JsonWebToken.cs
            var a = tokenHandler.ReadToken(token);
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

            return (JwtSecurityToken)null;
        }

        public static async Task<JwtSecurityToken> ValidateToken(HttpClient client, string token, string validAudience, string validIssuer)
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
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidAudience = validAudience,
                ValidIssuer = validIssuer,
                ValidateIssuerSigningKey = true,

                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidateAudience = true,
                ClockSkew = TimeSpan.Zero
            }, out var securityToken);

            return (JwtSecurityToken)securityToken;
        }
    }
}
