using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rinsen.Outback.Configuration
{
    public class OutbackOptions
    {

        public bool UseDefaultConfigurationAccessor { get; set; } = true;

        public bool DeviceAuthorizationGrantActive { get; set; }
    }
}
