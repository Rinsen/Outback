using System.Threading.Tasks;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Clients
{
    public interface IClientService
    {
        Task<Client> GetClient(AuthorizeModel model);
        Task<Client> GetClient(ClientIdentity clientIdentity);
    }
}