using System.Collections.Generic;

namespace Rinsen.Outback.App.Models
{
    public class DeviceConsentViewModel
    {
        public string UserCode { get; set; } = string.Empty;

        public List<DeviceConsentScopeModel> Scopes { get; set; } = new List<DeviceConsentScopeModel>();

        /// <summary>
        /// True when the user authorizes the device, false when the user rejects it.
        /// Submitted by the Authorize/Reject buttons on the consent screen.
        /// </summary>
        public bool Authorize { get; set; }
    }

    public class DeviceConsentScopeModel
    {
        public string Name { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;
    
        public string Description { get; set; } = string.Empty;
    }
}
