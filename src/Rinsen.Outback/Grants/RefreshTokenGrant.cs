using System;

namespace Rinsen.Outback.Grants
{
    public class RefreshTokenGrant
    {
        public string ClientId { get; set; }

        public string SubjectId { get; set; }

        public string RefreshToken { get; set; }

        public string Scope { get; set; }

        public DateTime Created { get; set; }

        public DateTime? Resolved { get; set; }

        public DateTime Expires { get; set; }
    }
}
