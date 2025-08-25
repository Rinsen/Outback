using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Configuration;
using Rinsen.Outback.Helpers;
using Rinsen.Outback.JwtTokens;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Grants
{
    internal class RefreshTokenGrantTypeHandler : IGrantTypeHandler
    {
        private readonly IGrantAccessor _grantAccessor;
        private readonly ITokenService _tokenService;
        private readonly RandomStringGenerator _randomStringGenerator;
        private readonly ILogger<RefreshTokenGrantTypeHandler> _logger;

        public RefreshTokenGrantTypeHandler(
            IGrantAccessor grantAccessor,
            ITokenService tokenService,
            RandomStringGenerator randomStringGenerator,
            ILogger<RefreshTokenGrantTypeHandler> logger)
        {
            _grantAccessor = grantAccessor;
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
                _logger.LogError(e, "Failed to resolve grant for client {ClientId} with {RefreshToken}", client.ClientId, tokenModel.RefreshToken);

                return new TokenResponse { ErrorResponse = new ErrorResponse { Error = ErrorResponses.InvalidGrant } };
            }

            var refreshToken = await CreateNewRefreshTokenAsync(client, refreshTokenGrant);

            var tokenResponse = await _tokenService.CreateTokenResponseAsync(client, refreshTokenGrant, refreshToken, issuer);

            return new TokenResponse { AccessTokenResponse = tokenResponse };
        }

        private async Task<RefreshTokenGrant> GetRefreshTokenGrantAsync(string refreshToken, string clientId)
        {
            var refreshTokenGrant = await _grantAccessor.GetRefreshTokenGrantAsync(refreshToken);

            if (refreshTokenGrant == default)
            {
                throw new SecurityException($"No RefreshToken found");
            }

            if (!string.Equals(refreshTokenGrant.ClientId, clientId))
            {
                throw new SecurityException($"RefreshToken is not valid for client");
            }

            return refreshTokenGrant;
        }

        public async Task<string> CreateNewRefreshTokenAsync(Client client, RefreshTokenGrant refreshTokenGrant)
        {
            var refreshToken = _randomStringGenerator.GetRandomString(40);
            var newRefreshTokenGrant = new RefreshTokenGrant
            {
                ClientId = client.ClientId,
                Created = DateTimeOffset.UtcNow,
                Expires = DateTimeOffset.UtcNow.AddSeconds(client.RefreshTokenLifetime),
                RefreshToken = refreshToken,
                Scope = refreshTokenGrant.Scope,
                SubjectId = refreshTokenGrant.SubjectId,
            };

            await _grantAccessor.SaveRefreshTokenGrantAsync(newRefreshTokenGrant);

            return refreshToken;
        }
    }
}
