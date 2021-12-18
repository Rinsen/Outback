using Rinsen.Outback.Accessors;

namespace SampleServer.InMemoryAccessors
{
    public class AllowedCorsOriginsAccessor : IAllowedCorsOriginsAccessor
    {
        public Task<HashSet<string>> GetOrigins()
        {
            throw new NotImplementedException();
        }
    }
}
