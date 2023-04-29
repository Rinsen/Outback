using System.Security.Claims;
using System.Threading.Tasks;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Grants;

public interface IGrantService
{
    Task<string> CreateCodeAndStoreCodeGrantAsync(Client client, ClaimsPrincipal user, AuthorizeModel model);
    Task<string> CreateRefreshTokenAsync(Client client, CodeGrant persistedGrant);
    Task<string> GetCodeForExistingConsentAsync(Client client, ClaimsPrincipal user, AuthorizeModel model);
    Task<CodeGrant> GetCodeGrantAsync(string code, string clientId, string codeVerifier);
    Task<RefreshTokenGrant> GetRefreshTokenGrantAsync(string refreshToken, string clientId);
    Task<string> CreateNewRefreshTokenAsync(Client client, RefreshTokenGrant refreshTokenGrant);
}
