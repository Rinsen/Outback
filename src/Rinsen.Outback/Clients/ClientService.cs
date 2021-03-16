using System;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Rinsen.Outback.Abstractons;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Clients
{
    public class ClientService
    {
        private readonly IClientAccessor _clientAccessor;

        public ClientService(IClientAccessor clientAccessor)
        {
            _clientAccessor = clientAccessor;
        }

        public async Task<Client> GetClient(AuthorizeModel model)
        {
            var client = await _clientAccessor.GetClient(model.ClientId);

            if (!ClientValidator.IsScopeValid(client, model.Scope))
            {
                throw new SecurityException();
            }

            if (!ClientValidator.IsRedirectUriValid(client, model.RedirectUri))
            {
                throw new SecurityException();
            }

            return client;
        }

        public async Task<Client> GetClient(ClientIdentity clientIdentity)
        {
            // Validate client secret if needed
            var client = await _clientAccessor.GetClient(clientIdentity.ClientId);

            switch (client.ClientType)
            {
                case ClientType.Confidential:
                case ClientType.Credentialed:
                    if (string.IsNullOrEmpty(clientIdentity.Secret))
                    {
                        throw new SecurityException($"No secret for client {clientIdentity.ClientId}");
                    }
                    
                    var secretHash = HashHelper.GetSha256Hash(clientIdentity.Secret);

                    if (!client.Secrets.Any(s => s == secretHash))
                    {
                        throw new SecurityException($"No valid secret for client {clientIdentity.ClientId}");
                    } 

                    return client;
                case ClientType.Public:
                    return client;
            }

            throw new Exception($"Client '{clientIdentity.ClientId}' not found");
        }
    }
}
