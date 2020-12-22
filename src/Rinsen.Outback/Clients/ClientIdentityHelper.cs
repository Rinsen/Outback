using System;
using System.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

namespace Rinsen.Outback.Clients
{
    public static class ClientIdentityHelper
    {
        public static ClientIdentity GetClientIdentity(TokenModel model, IHeaderDictionary headers)
        {
            // Basic auth or post paramaters for client auth and not both
            if (headers.ContainsKey("Authorization") && (!string.IsNullOrEmpty(model.ClientId) || !string.IsNullOrEmpty(model.ClientSecret)))
            {
                // Only one type of credentials is supported at the same time
                throw new SecurityException("Only one type of Authentication credentials is allowed for a client");
            }

            if (headers.ContainsKey("Authorization"))
            {
                var authHeaderValue = headers["Authorization"];

                if (authHeaderValue == StringValues.Empty)
                {
                    // Empty Authorization header is not supported
                    throw new SecurityException("Empty Authorization header is not supported");
                }

                var authHeaderString = authHeaderValue.ToString();
                if (!authHeaderString.StartsWith("Basic "))
                {
                    throw new SecurityException("Invalid basic auth header");
                }

                var encodedUsernameAndPassword = authHeaderString.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();

                var usernameAndPassword = Base64UrlEncoder.Decode(encodedUsernameAndPassword);

                var usernameAndPasswordParts = usernameAndPassword.Split(':', 2, StringSplitOptions.TrimEntries);

                if (usernameAndPasswordParts.Length > 1)
                {
                    return new ClientIdentity(usernameAndPasswordParts[0], usernameAndPasswordParts[1]);
                }
                else
                {
                    return new ClientIdentity(usernameAndPasswordParts[0], string.Empty);
                }
            }
            else if (!string.IsNullOrEmpty(model.ClientId))
            {
                return new ClientIdentity(model.ClientId, model.ClientSecret);
            }
            else
            {
                throw new SecurityException("No client credentials provided"); // No credentials
            }
        }
    }


}
