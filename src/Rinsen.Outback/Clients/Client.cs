using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rinsen.Outback.Clients
{
    public class Client
    {
        public string ClientId { get; set; }
        public bool ConsentRequired { get; set; }
        public bool IssueRefreshToken { get; set; }
        public int AccessTokenLifetime { get; set; }
        public int IdentityTokenLifetime { get; set; }

        public List<string> Scopes { get; set; }

        public List<string> RedirectUris { get; set; }

        public List<string> GrantTypes { get; set; }


    }
}
