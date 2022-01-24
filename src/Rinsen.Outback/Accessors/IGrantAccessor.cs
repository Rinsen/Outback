using System;
using System.Threading.Tasks;
using Rinsen.Outback.Grants;

namespace Rinsen.Outback.Accessors
{
    public interface IGrantAccessor
    {
        Task<CodeGrant> GetCodeGrantAsync(string code);
        Task<PersistedGrant> GetPersistedGrantAsync(string clientId, Guid subjectId);
        Task<RefreshTokenGrant> GetRefreshTokenGrantAsync(string refreshToken);
        Task SaveCodeGrantAsync(CodeGrant codeGrant);
        Task SavePersistedGrantAsync(PersistedGrant persistedGrant);
        Task SaveRefreshTokenGrantAsync(RefreshTokenGrant refreshTokenGrant);
    }
}
