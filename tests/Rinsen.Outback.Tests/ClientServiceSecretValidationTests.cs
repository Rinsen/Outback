using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Helpers;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// SEC-012: ClientService must validate the client secret with a constant-time
    /// comparison. A correct secret is accepted; an incorrect secret is rejected.
    /// </summary>
    public class ClientServiceSecretValidationTests
    {
        private const string ClientId = "test-client";
        private const string CorrectSecret = "correct-horse-battery-staple";
        private const string WrongSecret = "wrong-secret-value-entirely";

        [Fact]
        public async Task GetClient_CorrectSecret_ReturnsClient()
        {
            var service = CreateService(CorrectSecret);

            var client = await service.GetClient(new ClientIdentity(ClientId, CorrectSecret));

            Assert.Equal(ClientId, client.ClientId);
        }

        [Fact]
        public async Task GetClient_WrongSecret_ThrowsSecurityException()
        {
            var service = CreateService(CorrectSecret);

            await Assert.ThrowsAsync<SecurityException>(() =>
                service.GetClient(new ClientIdentity(ClientId, WrongSecret)));
        }

        private static ClientService CreateService(string storedSecret)
        {
            var client = new Client
            {
                ClientId = ClientId,
                ClientType = ClientType.Confidential,
                Secrets = new List<string> { HashHelper.GetSha256Hash(storedSecret) }
            };

            var accessor = new FakeClientAccessor(client);
            var logger = new LoggerFactory().CreateLogger<ClientService>();

            return new ClientService(accessor, logger);
        }

        private sealed class FakeClientAccessor : IClientAccessor
        {
            private readonly Client _client;

            public FakeClientAccessor(Client client)
            {
                _client = client;
            }

            public Task<Client> GetClient(string clientId)
            {
                return Task.FromResult(_client);
            }
        }
    }
}
