using System.Threading.Tasks;
using Rinsen.Outback.Grants;

namespace Rinsen.Outback.Accessors;

public interface IGrantAccessor
{
    Task<CodeGrant> GetCodeGrantAsync(string code);
    Task<PersistedGrant> GetPersistedGrantAsync(string clientId, string subjectId);
    Task<RefreshTokenGrant> GetRefreshTokenGrantAsync(string refreshToken);
    Task<DeviceAuthorizationGrant> GetDeviceAuthorizationGrantAsync(string deviceCode);
    Task SaveCodeGrantAsync(CodeGrant codeGrant);
    Task SavePersistedGrantAsync(PersistedGrant persistedGrant);
    Task SaveRefreshTokenGrantAsync(RefreshTokenGrant refreshTokenGrant);
    Task SaveDeviceAuthorizationGrantAsync(DeviceAuthorizationGrant deviceAuthorizationGrant);
}
