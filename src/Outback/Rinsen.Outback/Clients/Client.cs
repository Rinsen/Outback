using System.Collections.Generic;

namespace Rinsen.Outback.Clients;

public class Client
{
    /// <summary>
    /// Unique identifier for this client
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
    /// <summary>
    /// What type of client credentials this client uses.
    /// </summary>
    public ClientType ClientType { get; set; }

    /// <summary>
    /// Is consent required for this client
    /// </summary>
    public bool ConsentRequired { get; set; }

    /// <summary>
    /// Indicates if a client grant should be saved
    /// </summary>
    public bool SaveConsent { get; set; }

    /// <summary>
    /// For how long time should a consent be valid.
    /// <para>Value in minutes.</para>
    /// </summary>
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
    /// Lifetime in seconds
    /// </summary>
    public int DeviceCodeUserCompletionLifetime { get; set; }

    /// <summary>
    /// Lifetime in seconds, maximum recommended value is 600 but default is 60 seconds
    /// </summary>
    public int AuthorityCodeLifetime { get; set; } = 60;
    /// <summary>
    /// If this is checked user claims will be added in identity token
    /// </summary>
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
    /// <summary>
    /// Clients that have credentials with the AS are designated as "confidential clients"
    /// </summary>
    Confidential,
    /// <summary>
    /// Clients without credentials are called "public clients"
    /// </summary>
    Public
}
