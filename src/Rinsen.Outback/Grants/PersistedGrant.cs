using System;

namespace Rinsen.Outback.Grants
{
    public class PersistedGrant
    {
        public string ClientId { get; set; }

        public string SubjectId { get; set; }

        public string Scope { get; set; }

        public DateTime Created { get; set; }

        public DateTime Expires { get; set; }
    }
}
