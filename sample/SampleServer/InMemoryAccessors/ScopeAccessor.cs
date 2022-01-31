using System.Linq;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Scopes;

namespace SampleServer.InMemoryAccessors
{
    public class ScopeAccessor : IScopeAccessor
    {
        public Task<List<Scope>> GetScopesAsync()
        {
            return Task.FromResult(new List<Scope>
                {
                    new Scope { ScopeName = "openid", Audience = "OutbackAS", ShowInDiscoveryDocument = true },
                    new Scope { ScopeName = "profile", Audience = "OutbackAS", ShowInDiscoveryDocument = true },
                    new Scope { ScopeName = "messaging", Audience = "MessagingServer" ,ShowInDiscoveryDocument = true },
                });
        }

        public async Task<List<Scope>> GetScopesAsync(IReadOnlyList<string> scopes)
        {
            var scopeResults = await GetScopesAsync();

            return scopeResults.Where(m => scopes.Contains(m.ScopeName)).ToList();
        }
    }
}
