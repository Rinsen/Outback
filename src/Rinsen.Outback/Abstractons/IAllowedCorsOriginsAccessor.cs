using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rinsen.Outback.Abstractons
{
    public interface IAllowedCorsOriginsAccessor
    {
        Task<HashSet<string>> GetOrigins();

    }
}
