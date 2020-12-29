using System;
using System.Linq;

namespace Rinsen.Outback.Clients
{
    public class ClientValidator
    {
        public static bool IsScopeValid(Client client, string requestScopes)
        {
            var scopes = requestScopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var scope in scopes)
            {
                if (!client.Scopes.Any(s => string.Equals(s, scope)))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsRedirectUriValid(Client client, string redirectUri)
        {
            return client.LoginRedirectUris.Any(r => string.Equals(r, redirectUri));
        }

        public static bool IsGrantTypeSupported(Client client, string grantType)
        {
            return client.SupportedGrantTypes.Any(gt => string.Equals(gt, grantType));
        }
    }
}
