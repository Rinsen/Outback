using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rinsen.Outback.Configuration
{
    public interface IOutbackConfigurationAccessor
    {
        Task<bool> IsDeviceAuthorizationGrantActiveAsync();
    }
}
