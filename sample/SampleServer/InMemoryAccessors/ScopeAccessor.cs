using Rinsen.Outback.Accessors;
using Rinsen.Outback.Scopes;

namespace SampleServer.InMemoryAccessors
{
    public class ScopeAccessor : IScopeAccessor
    {
        public Task<List<Scope>> GetScopes()
        {
            return Task.FromResult(new List<Scope>
                {
                    new Scope { ScopeName = "openid", ShowInDiscoveryDocument = true },
                    new Scope { ScopeName = "profile", ShowInDiscoveryDocument = true },
                });
        }
    }
}
