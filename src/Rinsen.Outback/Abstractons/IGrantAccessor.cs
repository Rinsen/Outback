using System.Threading.Tasks;
using Rinsen.Outback.Grants;

namespace Rinsen.Outback.Abstractons
{
    public interface IGrantAccessor
    {
        Task<CodeGrant> GetCodeGrant(string code);
        Task<PersistedGrant> GetPersistedGrant(string clientId, string subjectId);
        Task<RefreshTokenGrant> GetRefreshTokenGrant(string refreshToken);
        Task SaveCodeGrant(CodeGrant codeGrant);
        Task SavePersistedGrant(PersistedGrant persistedGrant);
        Task SaveRefreshTokenGrant(RefreshTokenGrant refreshTokenGrant);
    }
}
