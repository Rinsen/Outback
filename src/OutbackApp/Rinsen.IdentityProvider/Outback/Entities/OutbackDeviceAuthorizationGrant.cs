using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rinsen.IdentityProvider.Outback.Entities
{
    public class OutbackDeviceAuthorizationGrant
    {
        public int Id { get; set; }

        public int ClientId { get; set; }

        public Guid SubjectId { get; set; }

        public bool AccessIsRejected { get; set; } = false;

        public string DeviceCode { get; set; } = string.Empty;

        public string UserCode { get; set; } = string.Empty;

        public string Scope { get; set; } = string.Empty;

        public DateTimeOffset UserCodeExpiration { get; set; }

        public int Interval { get; set; }

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset? Accepted { get; set; }

        public virtual OutbackClient? Client { get; set; }
    }
}
