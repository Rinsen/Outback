using System.Threading.Tasks;
using Rinsen.Outback.Clients;

namespace Rinsen.Outback.Accessors;

public interface IClientAccessor
{
    Task<Client> GetClient(string clientId);
}
