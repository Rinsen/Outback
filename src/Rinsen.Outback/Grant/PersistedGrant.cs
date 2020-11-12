using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rinsen.Outback.Grant
{
    public class PersistedGrant
    {
        public string Code { get; set; }

        public string ClientId { get; set; }

        public string SubjectId { get; set; }

        public string CodeChallange { get; set; }

        public string CodeChallangeMethod { get; set; }

        public string Nonce { get; set; }

        public string RedirectUri { get; set; }

        public string Scope { get; set; }

        public string State { get; set; }

        public string ResponseType { get; set; }

    }
}
