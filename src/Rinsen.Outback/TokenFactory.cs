using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Rinsen.Outback.Abstractons;
using Rinsen.Outback.Claims;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Grants;
using Rinsen.Outback.Models;

namespace Rinsen.Outback
{
    public class TokenFactory
    {
        private readonly ITokenSigningAccessor _tokenSigningAccessor;

        public TokenFactory(ITokenSigningAccessor tokenSigningAccessor)
        {
            _tokenSigningAccessor = tokenSigningAccessor;
        }

        public async Task<AccessTokenResponse> CreateTokenResponse(ClaimsPrincipal claimsPrincipal, Client client, CodeGrant persistedGrant, string issuer)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = await _tokenSigningAccessor.GetSigningSecurityKey();

            var identity = (ClaimsIdentity)claimsPrincipal.Identity;
            var identityTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                TokenType = null,
                Expires = DateTime.UtcNow.AddSeconds(client.AccessTokenLifetime),
                Issuer = issuer,
                IssuedAt = DateTime.UtcNow,
                Audience = client.ClientId,
                SigningCredentials = new SigningCredentials(key.SecurityKey, key.Algorithm),
            };

            identityTokenDescriptor.Claims = new Dictionary<string, object> { { StandardClaims.Nonce, persistedGrant.Nonce } };

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
                SigningCredentials = new SigningCredentials(key.SecurityKey, key.Algorithm),
            };

            accessTokenDescriptor.Claims = new Dictionary<string, object> { { StandardClaims.ClientIdentifier, client.ClientId } };
            accessTokenDescriptor.Claims.Add(StandardClaims.Scope, persistedGrant.Scope);

            var accessToken = tokenHandler.CreateToken(accessTokenDescriptor);
            var tokenString = tokenHandler.WriteToken(accessToken);

            if (client.IssueRefreshToken)
            {
                // Create refresh token

                throw new NotImplementedException();

                //return new RefreshTokenResponse
                //{
                //    AccessToken = tokenString,
                //    IdentityToken = identityTokenString,
                //    ExpiresIn = client.AccessTokenLifetime,
                //    RefreshToken = string.Empty,
                //    TokenType = "Bearer"
                //};
            }

            return new IdentityTokenResponse
            {
                AccessToken = tokenString,
                IdentityToken = identityTokenString,
                ExpiresIn = client.AccessTokenLifetime,
                TokenType = "Bearer"
            };
        }

        internal async Task<AccessTokenResponse> CreateTokenResponse(Client client, string issuer)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = await _tokenSigningAccessor.GetSigningSecurityKey();

            var accessTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                            new Claim(StandardClaims.Subject, client.ClientId)
                }),
                TokenType = "at+jwt",
                Expires = DateTime.UtcNow.AddSeconds(client.IdentityTokenLifetime),
                Issuer = issuer,
                IssuedAt = DateTime.UtcNow,
                Audience = client.ClientId,
                SigningCredentials = new SigningCredentials(key.SecurityKey, key.Algorithm),
            };

            accessTokenDescriptor.Claims = new Dictionary<string, object> { { StandardClaims.ClientIdentifier, client.ClientId } };
            accessTokenDescriptor.Claims.Add(StandardClaims.Scope, string.Join(' ', client.Scopes));

            var accessToken = tokenHandler.CreateToken(accessTokenDescriptor);
            var tokenString = tokenHandler.WriteToken(accessToken);

            return new AccessTokenResponse
            {
                AccessToken = tokenString,
                ExpiresIn = client.AccessTokenLifetime,
                TokenType = "Bearer"
            };
        }
    }
}
