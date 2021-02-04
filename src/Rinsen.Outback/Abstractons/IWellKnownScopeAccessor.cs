using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rinsen.Outback.Abstractons
{
    public interface IWellKnownScopeAccessor
    {
        Task<List<string>> GetScopes();

    }
}
