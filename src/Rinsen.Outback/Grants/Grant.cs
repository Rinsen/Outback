using System;

namespace Rinsen.Outback.Grants
{
    public class Grant
    {
        public int GrantId { get; set; }

        public string ClientId { get; set; }

        public string SubjectId { get; set; }

        public string Code { get; set; }

        public DateTime? CodeResolved { get; set; }

        public DateTime? CodeExpires { get; set; }

        public string CodeChallange { get; set; }

        public string CodeChallangeMethod { get; set; }

        public string State { get; set; }

        public string Nonce { get; set; }

        public string RedirectUri { get; set; }

        public string Scope { get; set; }

        public string ResponseType { get; set; }

        public string RefreshToken { get; set; }

        public DateTime? RefreshTokenCreated { get; set; }

        public DateTime? RefreshTokenExpires { get; set; }

        public DateTime Created { get; set; }

        public DateTime? Expires { get; set; }
    }
}
