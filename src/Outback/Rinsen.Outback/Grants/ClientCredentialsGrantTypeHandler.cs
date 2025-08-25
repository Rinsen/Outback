using System;
using System.Threading.Tasks;
using Rinsen.Outback.Clients;
using Rinsen.Outback.JwtTokens;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Grants
{
    internal class ClientCredentialsGrantTypeHandler : IGrantTypeHandler
    {
        private readonly ITokenService _tokenService;

        public ClientCredentialsGrantTypeHandler(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        public string GrantType => throw new NotImplementedException();

        public async Task<TokenResponse> GetTokenAsync(TokenModel tokenModel, Client client, string issuer)
        {
            var tokenResponse = await _tokenService.CreateTokenResponseAsync(client, issuer);

            if (tokenResponse == default)
            {
                return new TokenResponse
                {
                    ErrorResponse = new ErrorResponse { Error = ErrorResponses.InvalidRequest }
                };
            }

            return new TokenResponse
            {
                AccessTokenResponse = tokenResponse,
            };
        }
    }
}
