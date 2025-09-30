using System;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Configuration;
using Rinsen.Outback.Helpers;
using Rinsen.Outback.JwtTokens;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Grants
{
    internal class AuthorizationCodeGrantTypeHandler : IGrantTypeHandler
    {
        private readonly IOutbackConfigurationAccessor _outbackConfigurationAccessor;
        private readonly ITokenService _tokenFactory;
        private readonly IGrantAccessor _grantAccessor;
        private readonly RandomStringGenerator _randomStringGenerator;
        private readonly ILogger<AuthorizationCodeGrantTypeHandler> _logger;

        public AuthorizationCodeGrantTypeHandler(
            IOutbackConfigurationAccessor outbackConfigurationAccessor,
            ITokenService tokenFactory,
            IGrantAccessor grantAccessor,
            RandomStringGenerator randomStringGenerator,
            ILogger<AuthorizationCodeGrantTypeHandler> logger)
        {
            _outbackConfigurationAccessor = outbackConfigurationAccessor;
            _tokenFactory = tokenFactory;
            _grantAccessor = grantAccessor;
            _randomStringGenerator = randomStringGenerator;
            _logger = logger;
        }

        public string GrantType => "authorization_code";

        public async Task<TokenResponse> GetTokenAsync(TokenModel tokenModel, Client client, string issuer)
        {
            if (!await _outbackConfigurationAccessor.IsCodeGrantActiveAsync())
            {
                _logger.LogError("Code grant is not active for client {ClientId}", client.ClientId);
                return new TokenResponse { ErrorResponse = new ErrorResponse { Error = ErrorResponses.InvalidGrant } };
            }

            if (string.IsNullOrEmpty(tokenModel.Code))
            {
                _logger.LogError("No code for client {ClientId}", client.ClientId);
                return new TokenResponse { ErrorResponse = new ErrorResponse { Error = ErrorResponses.InvalidRequest } };
            }

            if (string.IsNullOrEmpty(tokenModel.CodeVerifier))
            {
                _logger.LogError("No code verifier for client {ClientId}", client.ClientId);
                return new TokenResponse { ErrorResponse = new ErrorResponse { Error = ErrorResponses.InvalidRequest } };
            }

            if (!AbnfValidationHelper.IsValid(tokenModel.CodeVerifier, 43, 128))
            {
                _logger.LogError("Code verifier is not ABNF valid for client {ClientId} with code verifier {CodeVerifier}", client.ClientId, tokenModel.CodeVerifier);
                return new TokenResponse { ErrorResponse = new ErrorResponse { Error = ErrorResponses.InvalidRequest } };
            }

            AuthorizationCodeGrant persistedGrant;
            try
            {
                persistedGrant = await GetCodeGrantAsync(tokenModel.Code, client.ClientId, tokenModel.CodeVerifier);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to resolve grant for client {ClientId}", client.ClientId);
                return new TokenResponse { ErrorResponse = new ErrorResponse { Error = ErrorResponses.InvalidGrant } };
            }

            // Validate return url if provided (same behavior as ConnectController)
            if (!string.Equals(persistedGrant.RedirectUri, tokenModel.RedirectUri))
            {
                _logger.LogError("Redirect uri '{RedirectUri}' did not match granted '{GrantedRedirectUri}' redirect uri", tokenModel.RedirectUri, persistedGrant.RedirectUri);
                return new TokenResponse { ErrorResponse = new ErrorResponse { Error = ErrorResponses.InvalidGrant } };
            }

            AccessTokenResponse accessTokenResponse;
            if (client.IssueRefreshToken)
            {
                var refreshToken = await CreateRefreshTokenAsync(client, persistedGrant);
                accessTokenResponse = await _tokenFactory.CreateTokenResponseAsync(client, persistedGrant, refreshToken, issuer);
            }
            else
            {
                accessTokenResponse = await _tokenFactory.CreateTokenResponseAsync(client, persistedGrant, issuer);
            }

            return new TokenResponse { AccessTokenResponse = accessTokenResponse };
        }

        public async Task<string> CreateRefreshTokenAsync(Client client, AuthorizationCodeGrant persistedGrant)
        {
            var refreshToken = _randomStringGenerator.GetRandomString(40);
            var refreshTokenGrant = new RefreshTokenGrant
            {
                ClientId = client.ClientId,
                Created = DateTimeOffset.UtcNow,
                Expires = DateTimeOffset.UtcNow.AddSeconds(client.RefreshTokenLifetime),
                RefreshToken = refreshToken,
                Scope = persistedGrant.Scope,
                SubjectId = persistedGrant.SubjectId,
            };

            await _grantAccessor.SaveRefreshTokenGrantAsync(refreshTokenGrant);

            return refreshToken;
        }

        private async Task<AuthorizationCodeGrant> GetCodeGrantAsync(string code, string clientId, string codeVerifier)
        {
            var codeGrant = await _grantAccessor.GetCodeGrantAsync(code);

            if (codeGrant.Expires < DateTimeOffset.UtcNow)
            {
                throw new SecurityException("Grant has expired");
            }

            if (codeGrant.ClientId != clientId)
            {
                throw new SecurityException("Client id not matching");
            }

            // Validate code
            var challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
            var codeChallenge = WebEncoders.Base64UrlEncode(challengeBytes);

            if (codeChallenge != codeGrant.CodeChallenge)
            {
                throw new SecurityException("Code verifier is not matching code challenge");
            }

            return codeGrant;
        }

        public Task<string> GetCodeForExistingConsentAsync(Client client, ClaimsPrincipal user, AuthorizeModel model)
        {
            throw new NotImplementedException();
        }

        public async Task<string> CreateCodeAndStoreCodeGrantAsync(Client client, ClaimsPrincipal user, AuthorizeModel model)
        {
            if (!AbnfValidationHelper.IsValid(model.CodeChallenge, 43, 128))
            {
                // Code verifier is not valid
                throw new SecurityException("Code challenge is not valid");
            }

            var grant = new AuthorizationCodeGrant
            {
                ClientId = client.ClientId,
                Code = _randomStringGenerator.GetRandomString(15),
                CodeChallenge = model.CodeChallenge,
                CodeChallengeMethod = model.CodeChallengeMethod,
                Nonce = model.Nonce,
                RedirectUri = model.RedirectUri,
                Scope = model.Scope,
                State = model.State,
                Expires = DateTimeOffset.UtcNow.AddSeconds(client.AuthorityCodeLifetime),
                Created = DateTimeOffset.UtcNow
            };

            SetSubjectId(user, grant);

            await _grantAccessor.SaveCodeGrantAsync(grant);

            return grant.Code;
        }

        private static void SetSubjectId(ClaimsPrincipal user, AuthorizationCodeGrant grant)
        {
            if (!user.Claims.Any(m => m.Type == "sub"))
            {
                throw new SecurityException("sub claim not found");
            }

            var subjectId = user.FindFirstValue("sub");

            if (string.IsNullOrEmpty(subjectId))
            {
                throw new SecurityException("sub claim empty is not supported");
            }

            grant.SubjectId = subjectId;
        }
    }
}
