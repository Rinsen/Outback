using System.Collections.Generic;

namespace Rinsen.Outback.Scopes;

public class Scope
{
    public string ScopeName { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public bool ShowInDiscoveryDocument { get; set; }

    public List<ScopeClaim> Claims { get; set; } = new List<ScopeClaim>();
}
