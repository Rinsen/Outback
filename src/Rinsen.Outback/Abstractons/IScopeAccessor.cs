using System.Collections.Generic;
using System.Threading.Tasks;
using Rinsen.Outback.Scopes;

namespace Rinsen.Outback.Abstractons
{
    public interface IScopeAccessor
    {
        Task<List<Scope>> GetScopes();

    }
}
