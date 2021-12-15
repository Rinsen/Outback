using Rinsen.Outback.Accessors;
using Rinsen.Outback.Clients;

namespace SampleServer.InMemoryAccessors
{
    public class ClientAccessor : IClientAccessor
    {
        public Task<Client> GetClient(string clientId)
        {
            throw new NotImplementedException();
        }
    }
}
