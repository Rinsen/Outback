using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Rinsen.IdentityProvider.Backup.Model;
using Rinsen.IdentityProvider.Outback.Entities;
using Rinsen.Outback.Clients;

namespace Rinsen.IdentityProvider.Backup.Model
{
    public class BackupClient
    {
        public string ClientId { get; set; } = string.Empty;
        public bool Active { get; set; } = false;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ClientType ClientType { get; set; }
        public int ClientFamilyId { get; set; }
        public bool ConsentRequired { get; set; } = false;
        public bool SaveConsent { get; set; } = false;

        public bool AddUserInfoClaimsInIdentityToken { get; set; } = false;

        public bool IssueRefreshToken { get; set; } = false;

        public bool IssueIdentityToken { get; set; } = true;


        /// <summary>
        /// Default 30 days
        /// </summary>
        public int SavedConsentLifetime { get; set; } = 2592000;

        /// <summary>
        /// Default 30 days
        /// </summary>
        public int RefreshTokenLifetime { get; set; } = 2592000;

        /// <summary>
        /// Default 1 hour
        /// </summary>
        public int AccessTokenLifetime { get; set; } = 3600;

        /// <summary>
        /// Default 10 minutes
        /// </summary>
        public int IdentityTokenLifetime { get; set; } = 600;

        /// <summary>
        /// Default 10 minutes
        /// </summary>
        public int AuthorityCodeLifetime { get; set; } = 600;

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset Updated { get; set; }

        public DateTimeOffset? Deleted { get; set; }

        public string ClientFamilyName { get; set; } = string.Empty;

        public List<BackupClientClaim> ClientClaims { get; set; } = [];

        public List<OutbackClientSecret> Secrets { get; set; } = [];

        public List<OutbackClientScope> Scopes { get; set; } = [];

        public List<OutbackClientSupportedGrantType> SupportedGrantTypes { get; set; } = [];

        public List<BackupClientLoginRedirectUri> LoginRedirectUris { get; set; } = [];

        public List<BackupClientPostLogoutRedirectUri> PostLogoutRedirectUris { get; set; } = [];

        public List<OutbackClientAllowedCorsOrigin> AllowedCorsOrigins { get; set; } = [];

        [JsonIgnore]
        public List<OutbackCodeGrant> CodeGrants { get; set; } = [];

        [JsonIgnore]
        public List<OutbackPersistedGrant> PersistedGrants { get; set; } = [];

        [JsonIgnore]
        public List<OutbackRefreshTokenGrant> RefreshTokenGrants { get; set; } = [];

        [JsonIgnore]
        public List<BackupDeviceAuthorizationGrant> DeviceAuthorizationGrants { get; set; } = [];


    }
}
