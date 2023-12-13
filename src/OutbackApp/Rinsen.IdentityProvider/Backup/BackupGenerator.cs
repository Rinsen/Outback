using System.Linq;
using System.Threading.Tasks;
using Rinsen.IdentityProvider.Backup.Model;
using Rinsen.IdentityProvider.Outback;

namespace Rinsen.IdentityProvider.Backup
{
    public class BackupGenerator
    {
        private readonly ClientService _clientService;

        public BackupGenerator(ClientService clientService)
        {
            _clientService = clientService;
        }

        public async Task<BackupRoot> CreateBackup()
        {
            var backupRoot = new BackupRoot
            {
                Content = new BackupContent(),
                Description = "Backup of Rinsen.IdentityProvider",
                Version = "1.0",
                Name = "Outback"
            };

            var clients = await _clientService.GetAllAsync();

            foreach (var client in clients)
            {
                backupRoot.Content.Clients.Add(new BackupClient
                {
                    AccessTokenLifetime = client.AccessTokenLifetime,
                    Active = client.Active,
                    AddUserInfoClaimsInIdentityToken = client.AddUserInfoClaimsInIdentityToken,
                    AllowedCorsOrigins = client.AllowedCorsOrigins,
                    AuthorityCodeLifetime = client.AuthorityCodeLifetime,
                    ClientClaims = client.ClientClaims.Select(cc => new BackupClientClaim { Type = cc.Type, Description = cc.Description, Value = cc.Value }).ToList(),
                    ClientFamilyName = client.ClientFamily?.Name ?? string.Empty,
                    ClientFamilyId = client.ClientFamilyId,
                    ClientId = client.ClientId,
                    ClientType = client.ClientType,
                    ConsentRequired = client.ConsentRequired,
                    Created = client.Created,
                    Deleted = client.Deleted,
                    Description = client.Description,
                    DeviceAuthorizationGrants = client.DeviceAuthorizationGrants.Select(dag => new BackupDeviceAuthorizationGrant { AccessIsRejected = dag.AccessIsRejected, DeviceCode = dag.DeviceCode, Interval = dag.Interval, Scope = dag.Scope, SubjectId = dag.SubjectId, UserCode = dag.UserCode, UserCodeExpiration = dag.UserCodeExpiration }).ToList(),
                    IdentityTokenLifetime = client.IdentityTokenLifetime,
                    IssueIdentityToken = client.IssueIdentityToken,
                    IssueRefreshToken = client.IssueRefreshToken,
                    LoginRedirectUris = client.LoginRedirectUris.Select(lru => new BackupClientLoginRedirectUri { LoginRedirectUri = lru.LoginRedirectUri, Description = lru.Description}).ToList(),
                    Name = client.Name,
                    PostLogoutRedirectUris = client.PostLogoutRedirectUris.Select(plru => new BackupClientPostLogoutRedirectUri { PostLogoutRedirectUri = plru.PostLogoutRedirectUri, Description = plru.Description }).ToList(),
                    Updated = client.Updated,
                });
            }

            return backupRoot;
        }
    }
}
