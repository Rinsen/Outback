using System;
using System.Threading.Tasks;
using Rinsen.Outback.Grants;

namespace Rinsen.Outback.Accessors
{
    public interface IGrantAccessor
    {
        Task<CodeGrant> GetCodeGrant(string code);
        Task<PersistedGrant> GetPersistedGrant(string clientId, Guid subjectId);
        Task<RefreshTokenGrant> GetRefreshTokenGrant(string refreshToken);
        Task SaveCodeGrant(CodeGrant codeGrant);
        Task SavePersistedGrant(PersistedGrant persistedGrant);
        Task SaveRefreshTokenGrant(RefreshTokenGrant refreshTokenGrant);
    }
}
