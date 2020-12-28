using System.Threading.Tasks;
using Rinsen.Outback.Grants;

namespace Rinsen.Outback.Abstractons
{
    public interface IGrantStorage
    {
        Task<CodeGrant> GetCodeGrant(string code);
        Task<PersistedGrant> GetPersistedGrant(string code);
        Task<RefreshTokenGrant> GetRefreshTokenGrant(string code);
        Task SaveCodeGrant(CodeGrant codeGrant);
        Task SavePersistedGrant(PersistedGrant persistedGrant);
        Task SaveRefreshTokenGrant(RefreshTokenGrant refreshTokenGrant);
    }
}
