using System;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Helpers;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Clients;

internal class ClientService : IClientService
{
    private readonly IClientAccessor _clientAccessor;
    private readonly ILogger<ClientService> _logger;

    public ClientService(IClientAccessor clientAccessor,
        ILogger<ClientService> logger)
    {
        _clientAccessor = clientAccessor;
        _logger = logger;
    }

    public async Task<Client> GetClient(AuthorizeModel model)
    {
        var client = await _clientAccessor.GetClient(model.ClientId);

        return client;
    }

    public async Task<Client> GetClient(ClientIdentity clientIdentity)
    {
        // Validate client secret if needed
        var client = await _clientAccessor.GetClient(clientIdentity.ClientId);

        switch (client.ClientType)
        {
            case ClientType.Confidential:
                if (string.IsNullOrEmpty(clientIdentity.Secret))
                {
                    _logger.LogWarning("Secret is required for client {ClientId}", clientIdentity.ClientId);

                    throw new SecurityException($"Secret is required for client {clientIdentity.ClientId}");
                }

                var secretHash = HashHelper.GetSha256Hash(clientIdentity.Secret);

                if (!client.Secrets.Any(s => s == secretHash))
                {
                    _logger.LogWarning("No valid secret provided for client {ClientId}", clientIdentity.ClientId);

                    throw new SecurityException($"No valid secret provided for client {clientIdentity.ClientId}");
                }

                return client;
            case ClientType.Public:
                return client;
        }

        _logger.LogWarning("Client {ClientId} not found", clientIdentity.ClientId);

        throw new Exception($"Client '{clientIdentity.ClientId}' not found");
    }
}
