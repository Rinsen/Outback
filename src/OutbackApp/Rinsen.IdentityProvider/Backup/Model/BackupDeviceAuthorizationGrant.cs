using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rinsen.IdentityProvider.Backup.Model
{
    public class BackupDeviceAuthorizationGrant
    {
        public Guid SubjectId { get; set; }

        public bool AccessIsRejected { get; set; } = false;

        public string DeviceCode { get; set; } = string.Empty;

        public string UserCode { get; set; } = string.Empty;

        public string Scope { get; set; } = string.Empty;

        public DateTimeOffset UserCodeExpiration { get; set; }

        public int Interval { get; set; }

    }
}
