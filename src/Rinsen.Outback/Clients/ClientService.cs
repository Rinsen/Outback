using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Clients
{
    public class ClientService
    {


        

        public Task<Client> GetClient(AuthorizeModel model)
        {
            var client = GetClientPrivate(model.ClientId);

            if (!ClientValidator.IsScopeValid(client, model.Scope))
            {
                throw new SecurityException();
            }

            if (!ClientValidator.IsRedirectUriValid(client, model.RedirectUri))
            {
                throw new SecurityException();
            }

            return Task.FromResult(client);
        }

        public Task<Client> GetClient(string clientId, string clientSecret)
        {
            // Validate client secret if needed
            var client = GetClientPrivate(clientId);

            switch (client.ClientType)
            {
                case ClientType.Confidential:
                    // Validate secret
                    break;
                case ClientType.Credentialed:
                    // Validate secret
                    break;
                case ClientType.Public:
                    throw new NotSupportedException();
                default:
                    break;
            }


            return Task.FromResult(client);
        }

        private Client GetClientPrivate(string clientId)
        {
            return new Client
            {
                AccessTokenLifetime = 3600,
                ClientId = clientId,
                ClientType = ClientType.Confidential,
                ClientClaims = new List<ClientClaim>(),
                ConsentRequired = false,
                IdentityTokenLifetime = 300,
                IssueRefreshToken = false,
                PostLogoutRedirectUri = new List<string>(),
                Secrets = new List<string>(),
                Scopes = new List<string>
                {
                    "openid",
                    "profile"
                },
                RedirectUris = new List<string>
                {
                    "https://localhost:44372/signin-oidc"
                },
                GrantTypes = new List<string>
                {
                    "authorization_code"
                }
            };
        }
    }
}
