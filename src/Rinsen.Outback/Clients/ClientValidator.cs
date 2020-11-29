using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rinsen.Outback.Clients
{
    public class ClientValidator
    {

        public bool IsScopeValid(Client client, string scope)
        {
            throw new NotImplementedException();
        }

        public bool IsRedirectUriValid(Client client, string redirectUri)
        {
            throw new NotImplementedException();
        }

        public bool IsGrantTypeSupported(Client client, string grantType)
        {
            throw new NotImplementedException();
        }
    }
}
