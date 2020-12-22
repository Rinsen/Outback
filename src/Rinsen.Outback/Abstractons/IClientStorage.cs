using System.Threading.Tasks;
using Rinsen.Outback.Clients;

namespace Rinsen.Outback.Abstractons
{
    public interface IClientStorage
    {
        Task<Client> GetClient(string clientId);
    }
}
