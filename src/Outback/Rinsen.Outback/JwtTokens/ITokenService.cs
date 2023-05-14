using System.Threading.Tasks;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Grants;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.JwtTokens;

public interface ITokenService
{
    Task<AccessTokenResponse> CreateTokenResponseAsync(Client client, string issuer);

    Task<AccessTokenResponse> CreateTokenResponseAsync(Client client, DeviceAuthorizationGrant deviceAuthorizationGrant, string issuer);

    Task<AccessTokenResponse> CreateTokenResponseAsync(Client client, CodeGrant persistedGrant, string issuer);

    Task<AccessTokenResponse> CreateTokenResponseAsync(Client client, CodeGrant persistedGrant, string refreshToken, string issuer);

    Task<AccessTokenResponse> CreateTokenResponseAsync(Client client, RefreshTokenGrant refreshTokenGrant, string refreshToken, string issuer);
}
