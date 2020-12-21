using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Cryptography;
using Rinsen.Outback.Grants;
using Rinsen.Outback.Models;

namespace Rinsen.Outback
{
    public class TokenFactory
    {
        private readonly EllipticCurveJsonWebKeyService _ellipticCurveJsonWebKeyService;

        public TokenFactory(EllipticCurveJsonWebKeyService ellipticCurveJsonWebKeyService)
        {
            _ellipticCurveJsonWebKeyService = ellipticCurveJsonWebKeyService;
        }

        public TokenResponse CreateTokenResponse(ClaimsPrincipal claimsPrincipal, Client client, Grant persistedGrant, string issuer)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var identity = (ClaimsIdentity)claimsPrincipal.Identity;
            var identityTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                TokenType = null,
                Expires = DateTime.UtcNow.AddSeconds(client.AccessTokenLifetime),
                Issuer = issuer,
                IssuedAt = DateTime.UtcNow,
                Audience = client.ClientId,
                SigningCredentials = new SigningCredentials(_ellipticCurveJsonWebKeyService.GetECDsaSecurityKey(), SecurityAlgorithms.EcdsaSha256),
            };

            identityTokenDescriptor.Claims = new Dictionary<string, object> { { "nonce", persistedGrant.Nonce } };

            var identityToken = tokenHandler.CreateToken(identityTokenDescriptor);
            var identityTokenString = tokenHandler.WriteToken(identityToken);

            var accessTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                TokenType = "at+jwt",
                Expires = DateTime.UtcNow.AddSeconds(client.IdentityTokenLifetime),
                Issuer = issuer,
                IssuedAt = DateTime.UtcNow,
                Audience = client.ClientId,
                SigningCredentials = new SigningCredentials(_ellipticCurveJsonWebKeyService.GetECDsaSecurityKey(), SecurityAlgorithms.EcdsaSha256),
            };

            accessTokenDescriptor.Claims = new Dictionary<string, object> { { "client_id", client.ClientId } };
            accessTokenDescriptor.Claims.Add("scope", persistedGrant.Scope);

            var accessToken = tokenHandler.CreateToken(accessTokenDescriptor);
            var tokenString = tokenHandler.WriteToken(accessToken);

            if (client.IssueRefreshToken)
            {
                return new RefreshTokenResponse
                {
                    AccessToken = tokenString,
                    IdentityToken = identityTokenString,
                    ExpiresIn = client.AccessTokenLifetime,
                    RefreshToken = persistedGrant.RefreshToken,
                    TokenType = "Bearer"
                };
            }

            return new TokenResponse
            {
                AccessToken = tokenString,
                IdentityToken = identityTokenString,
                ExpiresIn = client.AccessTokenLifetime,
                TokenType = "Bearer"
            };
        }

    }
}
