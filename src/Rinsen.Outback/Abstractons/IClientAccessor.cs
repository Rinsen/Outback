using System.Threading.Tasks;
using Rinsen.Outback.Clients;

namespace Rinsen.Outback.Abstractons
{
    public interface IClientAccessor
    {
        Task<Client> GetClient(string clientId);
    }
}
