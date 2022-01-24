using System;

namespace Rinsen.Outback.Grants
{
    public class CodeGrant
    {
        public string ClientId { get; set; }

        public string SubjectId { get; set; }

        public string Code { get; set; }

        public string CodeChallange { get; set; }

        public string CodeChallangeMethod { get; set; }

        public string State { get; set; }

        public string Nonce { get; set; }

        public string RedirectUri { get; set; }

        public string Scope { get; set; }

        public DateTime Created { get; set; }

        public DateTime Expires { get; set; }
    }
}
