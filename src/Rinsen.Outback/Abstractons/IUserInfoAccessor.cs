using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rinsen.Outback.Abstractons
{
    public interface IUserInfoAccessor
    {
        Task<Dictionary<string, string>> GetUserInfoClaims(string subjectId, IEnumerable<string> scopes);
    }
}
