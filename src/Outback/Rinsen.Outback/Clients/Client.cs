using System.Collections.Generic;

namespace Rinsen.Outback.Clients;

public class Client
{
    public string ClientId { get; set; } = string.Empty;
    public ClientType ClientType { get; set; }
    public bool ConsentRequired { get; set; }
    public bool SaveConsent { get; set; }
    public int SavedConsentLifetime { get; set; }

    /// <summary>
    /// Controls if an refresh_token is added to the Token response
    /// </summary>
    public bool IssueRefreshToken { get; set; }

    /// <summary>
    /// Controls if an id_token is added to the Token response
    /// </summary>
    public bool IssueIdentityToken { get; set; }
    /// <summary>
    /// Lifetime in seconds
    /// </summary>
    public int RefreshTokenLifetime { get; set; }
    /// <summary>
    /// Lifetime in seconds
    /// </summary>
    public int AccessTokenLifetime { get; set; }
    /// <summary>
    /// Lifetime in seconds
    /// </summary>
    public int IdentityTokenLifetime { get; set; }
    /// <summary>
    /// Lifetime in seconds, maximum recommended value is 600 but default is 60 seconds
    /// </summary>
    public int AuthorityCodeLifetime { get; set; } = 60;
    public bool AddUserInfoClaimsInIdentityToken { get; set; }

    public List<ClientClaim> ClientClaims { get; set; } = new List<ClientClaim>();

    public List<string> Secrets { get; set; } = new List<string>();

    public List<string> Scopes { get; set; } = new List<string>();

    public List<string> SupportedGrantTypes { get; set; } = new List<string>();

    public List<string> LoginRedirectUris { get; set; } = new List<string>();

    public List<string> PostLogoutRedirectUris { get; set; } = new List<string>();

    public List<string> AllowedCorsOrigins { get; set; } = new List<string>();

}

public enum ClientType : byte
{
    Confidential,
    Credentialed,
    Public
}
