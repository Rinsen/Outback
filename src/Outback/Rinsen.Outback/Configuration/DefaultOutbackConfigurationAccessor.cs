using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rinsen.Outback.Configuration
{
    internal class DefaultOutbackConfigurationAccessor : IOutbackConfigurationAccessor
    {
        private readonly OutbackOptions _outbackOptions;

        public DefaultOutbackConfigurationAccessor(OutbackOptions outbackOptions)
        {
            _outbackOptions = outbackOptions;
        }

        public Task<bool> IsDeviceAuthorizationGrantActiveAsync()
        {
            return Task.FromResult(_outbackOptions.DeviceAuthorizationGrantActive);
        }
    }
}
