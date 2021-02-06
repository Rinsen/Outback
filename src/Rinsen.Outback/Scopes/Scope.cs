using System.Collections.Generic;

namespace Rinsen.Outback.Scopes
{
    public class Scope
    {
        public string ScopeName { get; set; }

        public bool ShowInDiscoveryDocument { get; set; }

        public IReadOnlyList<ScopeClaim> Claims { get; set; }
    }
}
