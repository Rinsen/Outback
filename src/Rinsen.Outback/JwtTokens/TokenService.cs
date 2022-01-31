using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Claims;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Grants;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.JwtTokens;

internal class TokenService : ITokenService
{
    private readonly ITokenSigningAccessor _tokenSigningAccessor;
    private readonly IUserInfoAccessor _userInfoAccessor;
    private readonly IScopeAccessor _scopeAccessor;

    public TokenService(ITokenSigningAccessor tokenSigningAccessor,
        IUserInfoAccessor userInfoAccessor,
        IScopeAccessor scopeAccessor
        )
    {
        _tokenSigningAccessor = tokenSigningAccessor;
        _userInfoAccessor = userInfoAccessor;
        _scopeAccessor = scopeAccessor;
    }

    public Task<AccessTokenResponse> CreateTokenResponseAsync(Client client, CodeGrant persistedGrant, string issuer)
    {
        return CreateTokenResponseAsync(client, persistedGrant, null, issuer);
    }

    public async Task<AccessTokenResponse> CreateTokenResponseAsync(Client client, CodeGrant persistedGrant, string? refreshToken, string issuer)
    {
        var tokenHandler = new JsonWebTokenHandler();
        var key = await _tokenSigningAccessor.GetSigningSecurityKey();
        var accessTokenString = await CreateAccessToken(client, persistedGrant.Scope, persistedGrant.SubjectId, issuer, tokenHandler, key);

        if (client.IssueRefreshToken && client.IssueIdentityToken)
        {
            var identityTokenString = await CreateIdentityToken(client, persistedGrant.SubjectId, persistedGrant.Nonce, persistedGrant.Scope, issuer, tokenHandler, key);

            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new Exception($"No refresh token defined for client {client.ClientId}");
            }

            return new AccessTokenWithIdentityTokenAndRefreshTokenResponse
            {
                AccessToken = accessTokenString,
                ExpiresIn = client.AccessTokenLifetime,
                RefreshToken = refreshToken,
                IdentityToken = identityTokenString,
                TokenType = "Bearer"
            };
        }
        else if (client.IssueIdentityToken)
        {
            var identityTokenString = await CreateIdentityToken(client, persistedGrant.SubjectId, persistedGrant.Nonce, persistedGrant.Scope, issuer, tokenHandler, key);

            return new AccessTokenWithIdentityTokenResponse
            {
                AccessToken = accessTokenString,
                ExpiresIn = client.AccessTokenLifetime,
                IdentityToken = identityTokenString,
                TokenType = "Bearer"
            };
        }
        else if (client.IssueRefreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new Exception($"No refresh token defined for client {client.ClientId}");
            }

            return new AccessTokenWithRefreshTokenResponse
            {
                AccessToken = accessTokenString,
                ExpiresIn = client.AccessTokenLifetime,
                RefreshToken = refreshToken,
                TokenType = "Bearer"
            };
        }
        else
        {
            return new AccessTokenResponse
            {
                AccessToken = accessTokenString,
                ExpiresIn = client.AccessTokenLifetime,
                TokenType = "Bearer"
            };
        }
    }

    private async Task<string> CreateAccessToken(Client client, string scope, string subjectId, string issuer, JsonWebTokenHandler tokenHandler, SecurityKeyWithAlgorithm key)
    {
        var audience = await GetAudience(client);

        var accessTokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = null,
            TokenType = "at+jwt",
            Expires = DateTime.UtcNow.AddSeconds(client.IdentityTokenLifetime),
            Issuer = issuer,
            IssuedAt = DateTime.UtcNow,
            Audience = audience,
            SigningCredentials = new SigningCredentials(key.SecurityKey, key.Algorithm),
        };

        accessTokenDescriptor.Claims = new Dictionary<string, object>
            {
                { StandardClaims.ClientIdentifier, client.ClientId },
                { StandardClaims.Scope, scope },
                { StandardClaims.Subject, subjectId }
            };

        return tokenHandler.CreateToken(accessTokenDescriptor);
    }

    private async Task<string> CreateIdentityToken(Client client, string subjectId, string? nonce, string scope, string issuer, JsonWebTokenHandler tokenHandler, SecurityKeyWithAlgorithm key)
    {
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
                { StandardClaims.Subject, subjectId }
            };

        if (!string.IsNullOrEmpty(nonce))
        {
            identityTokenDescriptor.Claims.Add(StandardClaims.Nonce, nonce);
        }

        if (client.AddUserInfoClaimsInIdentityToken)
        {
            var scopes = scope.Split(' ');
            var claims = await _userInfoAccessor.GetUserInfoClaims(subjectId, scopes);

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

        return tokenHandler.CreateToken(identityTokenDescriptor);
    }

    public async Task<AccessTokenResponse> CreateTokenResponseAsync(Client client, string issuer)
    {
        var tokenHandler = new JsonWebTokenHandler();
        var key = await _tokenSigningAccessor.GetSigningSecurityKey();

        var audience = await GetAudience(client);

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
            Audience = audience,
            SigningCredentials = new SigningCredentials(key.SecurityKey, key.Algorithm),
        };

        accessTokenDescriptor.Claims = new Dictionary<string, object>
            {
                { StandardClaims.ClientIdentifier, client.ClientId },
                { StandardClaims.Scope, string.Join(' ', client.Scopes) }
            };

        var accessToken = tokenHandler.CreateToken(accessTokenDescriptor);
        
        return new AccessTokenResponse
        {
            AccessToken = accessToken,
            ExpiresIn = client.AccessTokenLifetime,
            TokenType = "Bearer"
        };
    }

    private async Task<string> GetAudience(Client client)
    {
        var scopes = await _scopeAccessor.GetScopesAsync(client.Scopes);

        var audiences = scopes.Select(m => m.Audience).Distinct().ToList();

        if (!audiences.Any())
        {
            return "[]";
        }

        if (audiences.Count == 1)
        {
            return audiences[0];
        }

        var audience = string.Join(", ", audiences);
        
        return audience;
    }

    public async Task<AccessTokenResponse> CreateTokenResponseAsync(Client client, RefreshTokenGrant refreshTokenGrant, string refreshToken, string issuer)
    {
        if (!client.IssueRefreshToken)
        {
            throw new Exception($"No support for refresh tokens for client {client.ClientId}");
        }

        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new Exception($"No refresh token defined for client {client.ClientId}");
        }

        var tokenHandler = new JsonWebTokenHandler();
        var key = await _tokenSigningAccessor.GetSigningSecurityKey();
        var accessTokenString = await CreateAccessToken(client, refreshTokenGrant.Scope, refreshTokenGrant.SubjectId, issuer, tokenHandler, key);

        if (client.IssueIdentityToken)
        {
            var identityTokenString = await CreateIdentityToken(client, refreshTokenGrant.SubjectId, null, refreshTokenGrant.Scope, issuer, tokenHandler, key);

            return new AccessTokenWithIdentityTokenAndRefreshTokenResponse
            {
                AccessToken = accessTokenString,
                ExpiresIn = client.AccessTokenLifetime,
                RefreshToken = refreshToken,
                IdentityToken = identityTokenString,
                TokenType = "Bearer"
            };
        }

        return new AccessTokenWithRefreshTokenResponse
        {
            AccessToken = accessTokenString,
            ExpiresIn = client.AccessTokenLifetime,
            RefreshToken = refreshToken,
            TokenType = "Bearer"
        };
    }
}
