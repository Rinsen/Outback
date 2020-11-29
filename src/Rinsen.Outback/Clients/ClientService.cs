using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rinsen.Outback.Clients
{
    public class ClientService
    {
        public Task<Client> GetClient(string clientId, string clientSecret)
        {
            // Validate client secret if needed


            return Task.FromResult(new Client
            {
                ClientId = clientId
            });
        }

        public Task<Client> GetClient(string clientId)
        {


            return Task.FromResult(new Client
            {
                ClientId = clientId
            });
        }
    }
}
