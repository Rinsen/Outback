using System;
using System.Threading.Tasks;
using Rinsen.Outback.Grants;

namespace Rinsen.Outback.Accessors;

public interface ICodeGrantAccessor
{
    Task<AuthorizationCodeGrant> GetCodeGrantAsync(string code);
    Task<PersistedGrant> GetPersistedGrantAsync(string clientId, string subjectId);
    Task SaveCodeGrantAsync(AuthorizationCodeGrant codeGrant);
    Task SavePersistedGrantAsync(PersistedGrant persistedGrant);
}

public interface IRefreshTokenGrantAccessor
{
    Task<RefreshTokenGrant> GetRefreshTokenGrantAsync(string refreshToken);
    Task SaveRefreshTokenGrantAsync(RefreshTokenGrant refreshTokenGrant);
    Task RevokeRefreshTokenFamilyAsync(Guid familyId);
}

public interface IDeviceAuthorizationGrantAccessor
{
    Task<DeviceCodeGrant> GetDeviceAuthorizationGrantAsync(string deviceCode);
    Task SaveDeviceAuthorizationGrantAsync(DeviceCodeGrant deviceAuthorizationGrant);
    Task ResolveDeviceAuthorizationGrantAsync(string deviceCode);
}
