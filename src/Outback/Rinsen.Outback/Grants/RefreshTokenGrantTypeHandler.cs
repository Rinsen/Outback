using System;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Helpers;
using Rinsen.Outback.JwtTokens;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Grants
{

    internal interface IRefreshTokenGrantTypeHandler
    {
        Task<string> CreateNewRefreshTokenAsync(Client client, string scope, string subjectId, Guid familyId);
    }


    internal class RefreshTokenGrantTypeHandler : IGrantTypeHandler, IRefreshTokenGrantTypeHandler
    {
        private readonly IRefreshTokenGrantAccessor _refreshTokenGrantAccessor;
        private readonly ITokenService _tokenService;
        private readonly RandomStringGenerator _randomStringGenerator;
        private readonly ILogger<RefreshTokenGrantTypeHandler> _logger;

        public RefreshTokenGrantTypeHandler(
            IRefreshTokenGrantAccessor refreshTokenGrantAccessor,
            ITokenService tokenService,
            RandomStringGenerator randomStringGenerator,
            ILogger<RefreshTokenGrantTypeHandler> logger)
        {
            _refreshTokenGrantAccessor = refreshTokenGrantAccessor;
            _tokenService = tokenService;
            _randomStringGenerator = randomStringGenerator;
            _logger = logger;
        }

        public string GrantType => "refresh_token";

        public async Task<TokenResponse> GetTokenAsync(TokenModel tokenModel, Client client, string issuer)
        {
            RefreshTokenGrant refreshTokenGrant;
            try
            {
                refreshTokenGrant = await GetRefreshTokenGrantAsync(tokenModel.RefreshToken, client.ClientId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to resolve grant for client {ClientId}", client.ClientId);

                return new TokenResponse { ErrorResponse = new ErrorResponse { Error = ErrorResponses.InvalidGrant } };
            }

            var refreshToken = await CreateNewRefreshTokenAsync(client, refreshTokenGrant.Scope, refreshTokenGrant.SubjectId, refreshTokenGrant.FamilyId);

            var tokenResponse = await _tokenService.CreateTokenResponseAsync(client, refreshTokenGrant, refreshToken, issuer);

            return new TokenResponse { AccessTokenResponse = tokenResponse };
        }

        private async Task<RefreshTokenGrant> GetRefreshTokenGrantAsync(string refreshToken, string clientId)
        {
            var refreshTokenGrant = await _refreshTokenGrantAccessor.GetRefreshTokenGrantAsync(refreshToken);

            if (refreshTokenGrant == default)
            {
                throw new SecurityException($"No RefreshToken found");
            }

            if (refreshTokenGrant.Resolved is not null)
            {
                // Reuse of an already-consumed token: revoke the entire token family (RFC 9700 §10.5)
                _logger.LogWarning("Refresh token reuse detected for client {ClientId}; revoking token family {FamilyId}", clientId, refreshTokenGrant.FamilyId);

                await _refreshTokenGrantAccessor.RevokeRefreshTokenFamilyAsync(refreshTokenGrant.FamilyId);

                throw new SecurityException("RefreshToken was already used");
            }

            if (refreshTokenGrant.Expires < DateTimeOffset.UtcNow)
            {
                throw new SecurityException("RefreshToken is expired");
            }

            if (!string.Equals(refreshTokenGrant.ClientId, clientId))
            {
                throw new SecurityException($"RefreshToken is not valid for client");
            }

            return refreshTokenGrant;
        }

        public async Task<string> CreateNewRefreshTokenAsync(Client client, string scope, string subjectId, Guid familyId)
        {
            var refreshToken = _randomStringGenerator.GetRandomString(40);
            var newRefreshTokenGrant = new RefreshTokenGrant
            {
                ClientId = client.ClientId,
                Created = DateTimeOffset.UtcNow,
                Expires = DateTimeOffset.UtcNow.AddSeconds(client.RefreshTokenLifetime),
                RefreshToken = refreshToken,
                Scope = scope,
                SubjectId = subjectId,
                FamilyId = familyId == Guid.Empty ? Guid.NewGuid() : familyId,
            };

            await _refreshTokenGrantAccessor.SaveRefreshTokenGrantAsync(newRefreshTokenGrant);

            return refreshToken;
        }
    }
}
