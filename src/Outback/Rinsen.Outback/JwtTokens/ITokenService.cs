using System.Threading.Tasks;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Grants;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.JwtTokens;

public interface ITokenService
{
    Task<AccessTokenResponse> CreateTokenResponseAsync(Client client, string issuer);

    Task<AccessTokenResponse> CreateTokenResponseAsync(Client client, DeviceCodeGrant deviceAuthorizationGrant, string issuer);

    Task<AccessTokenResponse> CreateTokenResponseAsync(Client client, AuthorizationCodeGrant persistedGrant, string issuer);

    Task<AccessTokenResponse> CreateTokenResponseAsync(Client client, AuthorizationCodeGrant persistedGrant, string refreshToken, string issuer);

    Task<AccessTokenResponse> CreateTokenResponseAsync(Client client, RefreshTokenGrant refreshTokenGrant, string refreshToken, string issuer);
}
