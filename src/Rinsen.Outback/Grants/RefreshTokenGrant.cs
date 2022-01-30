using System;

namespace Rinsen.Outback.Grants;

public class RefreshTokenGrant
{
    public string ClientId { get; set; } = string.Empty;

    public string SubjectId { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public DateTime Created { get; set; }

    public DateTime Expires { get; set; }
}
