using Rinsen.Outback.Accessors;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Helpers;

namespace SampleServer.InMemoryAccessors
{
    public class ClientAccessor : IClientAccessor
    {
        public Task<Client> GetClient(string clientId)
        {
            switch (clientId)
            {
                case "MachineToMachineClientId":
                    return Task.FromResult(new Client
                    {
                        AccessTokenLifetime = 100,
                        ClientClaims = new List<ClientClaim> { new ClientClaim { Type = "extra_claim", Value = "with value" } },
                        ClientId = clientId,
                        ClientType = ClientType.Confidential,
                        ConsentRequired = false,
                        IdentityTokenLifetime = 300,
                        IssueRefreshToken = false,
                        Secrets = new List<string>
                        {
                            HashHelper.GetSha256Hash("pwd")
                        },
                        Scopes = new List<string>
                        {
                            "messaging"
                        },
                        SupportedGrantTypes = new List<string>
                        {
                            "client_credentials"
                        }
                    });
                case "PKCEWebClientId":
                    return Task.FromResult(new Client
                    {
                        AccessTokenLifetime = 100,
                        ClientId = clientId,
                        ClientType = ClientType.Confidential,
                        ConsentRequired = false,
                        IdentityTokenLifetime = 300,
                        IssueIdentityToken = true,
                        IssueRefreshToken = false,
                        LoginRedirectUris = new List<string> { "https://my.domain/signin-oidc" },
                        Secrets = new List<string>
                        {
                            HashHelper.GetSha256Hash("pwd")
                        },
                        Scopes = new List<string>
                        {
                            "openid",
                            "profile",
                            "messaging"
                        },
                        SupportedGrantTypes = new List<string>
                        {
                            "authorization_code"
                        }
                    });
                case "DeviceAuthorizationClientId":
                    return Task.FromResult(new Client
                    {
                        AccessTokenLifetime = 100,
                        ClientId = clientId,
                        ClientType = ClientType.Public,
                        ConsentRequired = false,
                        IdentityTokenLifetime = 300,
                        IssueIdentityToken = true,
                        IssueRefreshToken = false,
                        LoginRedirectUris = new List<string>(),
                        Secrets = new List<string>(),
                        DeviceCodeUserCompletionLifetime = 100,
                        Scopes = new List<string>
                        {
                            "openid",
                            "profile",
                            "messaging"
                        },
                        SupportedGrantTypes = new List<string>
                        {
                            "urn:ietf:params:oauth:grant-type:device_code"
                        }
                    });
                default:
                    throw new Exception($"No client for client id '{clientId}' found");
            }
        }
    }
}
