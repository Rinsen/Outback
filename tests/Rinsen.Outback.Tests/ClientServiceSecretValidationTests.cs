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
    /// SEC-013: stored secrets use a salted KDF; legacy (unsalted) stored secrets
    /// still verify and trigger a re-hash to the salted format on success.
    /// </summary>
    public class ClientServiceSecretValidationTests
    {
        private const string ClientId = "test-client";
        private const string CorrectSecret = "correct-horse-battery-staple";
        private const string WrongSecret = "wrong-secret-value-entirely";

        [Fact]
        public async Task GetClient_CorrectSecret_ReturnsClient()
        {
            var service = CreateService(HashHelper.HashClientSecret(CorrectSecret));

            var client = await service.GetClient(new ClientIdentity(ClientId, CorrectSecret));

            Assert.Equal(ClientId, client.ClientId);
        }

        [Fact]
        public async Task GetClient_WrongSecret_ThrowsSecurityException()
        {
            var service = CreateService(HashHelper.HashClientSecret(CorrectSecret));

            await Assert.ThrowsAsync<SecurityException>(() =>
                service.GetClient(new ClientIdentity(ClientId, WrongSecret)));
        }

        [Fact]
        public async Task GetClient_LegacyStoredSecret_ReturnsClient()
        {
            var service = CreateService(HashHelper.GetSha256Hash(CorrectSecret));

            var client = await service.GetClient(new ClientIdentity(ClientId, CorrectSecret));

            Assert.Equal(ClientId, client.ClientId);
        }

        [Fact]
        public async Task GetClient_LegacyStoredSecret_TriggersRehash()
        {
            var rehasher = new CapturingRehasher();
            var legacyStored = HashHelper.GetSha256Hash(CorrectSecret);
            var service = CreateService(legacyStored, rehasher);

            await service.GetClient(new ClientIdentity(ClientId, CorrectSecret));

            var call = Assert.Single(rehasher.Calls);
            Assert.Equal(ClientId, call.ClientId);
            Assert.Equal(legacyStored, call.OldStored);
            Assert.StartsWith("PBKDF2-SHA256$", call.NewStored);
            Assert.True(HashHelper.VerifyClientSecret(CorrectSecret, call.NewStored));
        }

        [Fact]
        public async Task GetClient_SaltedStoredSecret_DoesNotTriggerRehash()
        {
            var rehasher = new CapturingRehasher();
            var service = CreateService(HashHelper.HashClientSecret(CorrectSecret), rehasher);

            await service.GetClient(new ClientIdentity(ClientId, CorrectSecret));

            Assert.Empty(rehasher.Calls);
        }

        private static ClientService CreateService(string storedSecret, IClientSecretRehasher rehasher = null)
        {
            var client = new Client
            {
                ClientId = ClientId,
                ClientType = ClientType.Confidential,
                Secrets = new List<string> { storedSecret }
            };

            var accessor = new FakeClientAccessor(client, rehasher);
            var logger = new LoggerFactory().CreateLogger<ClientService>();

            return new ClientService(accessor, logger);
        }

        private sealed class FakeClientAccessor : IClientAccessor, IClientSecretRehasher
        {
            private readonly Client _client;
            private readonly IClientSecretRehasher _rehasher;

            public FakeClientAccessor(Client client, IClientSecretRehasher rehasher)
            {
                _client = client;
                _rehasher = rehasher;
            }

            public Task<Client> GetClient(string clientId)
            {
                return Task.FromResult(_client);
            }

            public Task RehashClientSecretAsync(string clientId, string oldStored, string newStored)
            {
                if (_rehasher == null)
                {
                    return Task.CompletedTask;
                }

                return _rehasher.RehashClientSecretAsync(clientId, oldStored, newStored);
            }
        }

        private sealed class CapturingRehasher : IClientSecretRehasher
        {
            public sealed class Call
            {
                public string? ClientId { get; set; }
                public string? OldStored { get; set; }
                public string? NewStored { get; set; }
            }

            public List<Call> Calls { get; } = new List<Call>();

            public Task RehashClientSecretAsync(string clientId, string oldStored, string newStored)
            {
                Calls.Add(new Call { ClientId = clientId, OldStored = oldStored, NewStored = newStored });

                return Task.CompletedTask;
            }
        }
    }
}
