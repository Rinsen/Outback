using System.Collections.Generic;

namespace Rinsen.Outback.App.Models
{
    internal class DeviceConsentViewModel
    {
        public string UserCode { get; set; } = string.Empty;

        public List<DeviceConsentScopeModel> Scopes { get; set; } = new List<DeviceConsentScopeModel>();
    }

    internal class DeviceConsentScopeModel
    {
        public string Name { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;
    
        public string Description { get; set; } = string.Empty;
    }
}
