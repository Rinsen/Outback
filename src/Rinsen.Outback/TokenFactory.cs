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
        private readonly ITokenSigningStorage _tokenSigningStorage;

        public TokenFactory(ITokenSigningStorage tokenSigningStorage)
        {
            _tokenSigningStorage = tokenSigningStorage;
        }

        public async Task<TokenResponse> CreateTokenResponse(ClaimsPrincipal claimsPrincipal, Client client, Grant persistedGrant, string issuer)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityKey = await _tokenSigningStorage.GetSigningSecurityKey();
            var algorithm = await _tokenSigningStorage.GetSigningAlgorithm();

            var identity = (ClaimsIdentity)claimsPrincipal.Identity;
            var identityTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                TokenType = null,
                Expires = DateTime.UtcNow.AddSeconds(client.AccessTokenLifetime),
                Issuer = issuer,
                IssuedAt = DateTime.UtcNow,
                Audience = client.ClientId,
                SigningCredentials = new SigningCredentials(securityKey, algorithm),
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
                SigningCredentials = new SigningCredentials(securityKey, algorithm),
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
