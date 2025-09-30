using System.Threading.Tasks;
using Rinsen.Outback.Grants;

namespace Rinsen.Outback.Accessors;

public interface IGrantAccessor
{
    Task<AuthorizationCodeGrant> GetCodeGrantAsync(string code);
    Task<PersistedGrant> GetPersistedGrantAsync(string clientId, string subjectId);
    Task<RefreshTokenGrant> GetRefreshTokenGrantAsync(string refreshToken);
    Task<DeviceCodeGrant> GetDeviceAuthorizationGrantAsync(string deviceCode);
    Task SaveCodeGrantAsync(AuthorizationCodeGrant codeGrant);
    Task SavePersistedGrantAsync(PersistedGrant persistedGrant);
    Task SaveRefreshTokenGrantAsync(RefreshTokenGrant refreshTokenGrant);
    Task SaveDeviceAuthorizationGrantAsync(DeviceCodeGrant deviceAuthorizationGrant);
}
