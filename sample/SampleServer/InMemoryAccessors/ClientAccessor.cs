using Rinsen.Outback;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Clients;

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
                        ClientId = clientId,
                        ClientType = ClientType.Confidential,
                        ClientClaims = new List<ClientClaim>(),
                        ConsentRequired = false,
                        IdentityTokenLifetime = 300,
                        IssueRefreshToken = false,
                        PostLogoutRedirectUris = new List<string>(),
                        Secrets = new List<string>
                        {
                            HashHelper.GetSha256Hash("pwd")
                        },
                        Scopes = new List<string>
                        {
                            "openid",
                            "profile"
                        },
                        SupportedGrantTypes = new List<string>
                        {
                            "client_credentials"
                        }
                    });
                default:
                    throw new Exception($"No client for client id '{clientId}' found");
            }
        }
    }
}
