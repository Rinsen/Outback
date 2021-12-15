using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Claims;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Grants;
using Rinsen.Outback.Models;

namespace Rinsen.Outback
{
    public class TokenFactory
    {
        private readonly ITokenSigningAccessor _tokenSigningAccessor;
        private readonly IUserInfoAccessor _userInfoAccessor;

        public TokenFactory(ITokenSigningAccessor tokenSigningAccessor,
            IUserInfoAccessor userInfoAccessor
            )
        {
            _tokenSigningAccessor = tokenSigningAccessor;
            _userInfoAccessor = userInfoAccessor;
        }

        public async Task<AccessTokenResponse> CreateTokenResponse(Client client, CodeGrant persistedGrant, string issuer)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = await _tokenSigningAccessor.GetSigningSecurityKey();

            var identityTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = null,
                TokenType = null,
                Expires = DateTime.UtcNow.AddSeconds(client.AccessTokenLifetime),
                Issuer = issuer,
                IssuedAt = DateTime.UtcNow,
                Audience = client.ClientId,
                SigningCredentials = new SigningCredentials(key.SecurityKey, key.Algorithm),
            };

            identityTokenDescriptor.Claims = new Dictionary<string, object>
            {
                { StandardClaims.Subject, persistedGrant.SubjectId }
            };

            if (!string.IsNullOrEmpty(persistedGrant.Nonce))
            {
                identityTokenDescriptor.Claims.Add(StandardClaims.Nonce, persistedGrant.Nonce);
            }

            if (client.AddUserInfoClaimsInIdentityToken)
            {
                var scopes = persistedGrant.Scope.Split(' ');
                var claims = await _userInfoAccessor.GetUserInfoClaims(persistedGrant.SubjectId, scopes);

                foreach (var claim in claims)
                {
                    switch (claim.Key)
                    {
                        case StandardClaims.Issuer:
                        case StandardClaims.Subject:
                        case StandardClaims.Audience:
                        case StandardClaims.Expiration:
                        case StandardClaims.IssuedAt:
                        case StandardClaims.AuthenticationTime:
                        case StandardClaims.Nonce:
                        case StandardClaims.AuthenticationContextClassReference:
                        case StandardClaims.AuthorizedParty:
                            // Ignore OpenId standard claims
                            break;
                        default:
                            identityTokenDescriptor.Claims.Add(claim.Key, claim.Value);
                            break;
                    }
                }
            }

            var identityToken = tokenHandler.CreateToken(identityTokenDescriptor);
            var identityTokenString = tokenHandler.WriteToken(identityToken);

            var accessTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = null,
                TokenType = "at+jwt",
                Expires = DateTime.UtcNow.AddSeconds(client.IdentityTokenLifetime),
                Issuer = issuer,
                IssuedAt = DateTime.UtcNow,
                Audience = client.ClientId,
                SigningCredentials = new SigningCredentials(key.SecurityKey, key.Algorithm),
            };

            accessTokenDescriptor.Claims = new Dictionary<string, object> 
            { 
                { StandardClaims.ClientIdentifier, client.ClientId },
                { StandardClaims.Scope, persistedGrant.Scope },
                { StandardClaims.Subject, persistedGrant.SubjectId }
            };

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
