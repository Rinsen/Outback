using System.Collections.Generic;

namespace Rinsen.Outback.Clients
{
    public class Client
    {
        public string ClientId { get; set; }
        public ClientType ClientType { get; set; }
        public bool ConsentRequired { get; set; }
        public bool IssueRefreshToken { get; set; }
        public int AccessTokenLifetime { get; set; }
        public int IdentityTokenLifetime { get; set; }

        public List<ClientClaim> ClientClaims { get; set; }

        public List<string> Secrets { get; set; }

        public List<string> Scopes { get; set; }

        public List<string> RedirectUris { get; set; }

        public List<string> GrantTypes { get; set; }

        public List<string> PostLogoutRedirectUri { get; set; }

    }

    public enum ClientType
    {
        Confidential,
        Credentialed,
        Public
    }
}
