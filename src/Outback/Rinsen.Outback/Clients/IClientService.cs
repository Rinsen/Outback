using System.Threading.Tasks;

namespace Rinsen.Outback.Clients;

public interface IClientService
{
    Task<Client> GetClient(string clientId);
    Task<Client> GetClient(ClientIdentity clientIdentity);
}
