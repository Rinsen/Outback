using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rinsen.Outback.Accessors;

public interface IAllowedCorsOriginsAccessor
{
    Task<HashSet<string>> GetOrigins();

}
