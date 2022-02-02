using System;

namespace Rinsen.Outback.Grants;

public class PersistedGrant
{
    public string ClientId { get; set; } = string.Empty;

    public string SubjectId { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public DateTimeOffset Created { get; set; }

    public DateTimeOffset Expires { get; set; }
}
