using System.Collections.Generic;
using System.Threading.Tasks;
using Rinsen.Outback.Scopes;

namespace Rinsen.Outback.Accessors;

public interface IScopeAccessor
{
    Task<List<Scope>> GetScopesAsync();

    Task<List<Scope>> GetScopesAsync(IReadOnlyList<string> scopes);

}
