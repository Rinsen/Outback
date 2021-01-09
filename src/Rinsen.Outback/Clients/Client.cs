using System.Collections.Generic;

namespace Rinsen.Outback.Clients
{
    public class Client
    {
        public string ClientId { get; set; }
        public ClientType ClientType { get; set; }
        public bool ConsentRequired { get; set; }
        public bool SaveConsent { get; set; }
        public bool SavedConsentLifetime { get; set; }
        public bool IssueRefreshToken { get; set; }
        public int RefreshTokenLifetime { get; set; }
        public int AccessTokenLifetime { get; set; }
        public int IdentityTokenLifetime { get; set; }
        public int AuthorityCodeLifetime { get; set; }

        public List<ClientClaim> ClientClaims { get; set; }

        public List<string> Secrets { get; set; }

        public List<string> Scopes { get; set; }

        public List<string> SupportedGrantTypes { get; set; }

        public List<string> LoginRedirectUris { get; set; }

        public List<string> PostLogoutRedirectUri { get; set; }

        public List<string> AllowedCorsOrigins { get; set; }

    }

    public enum ClientType
    {
        Confidential,
        Credentialed,
        Public
    }
}
