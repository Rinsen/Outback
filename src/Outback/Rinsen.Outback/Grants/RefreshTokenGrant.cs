using System;

namespace Rinsen.Outback.Grants;

public class RefreshTokenGrant
{
    public string ClientId { get; set; } = string.Empty;

    public string SubjectId { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public DateTimeOffset Created { get; set; }

    public DateTimeOffset Expires { get; set; }

    /// <summary>
    /// Identifies the rotation family this grant belongs to. All grants created by
    /// rotating this grant, directly or transitively, share the same family id.
    /// </summary>
    public Guid FamilyId { get; set; }

    /// <summary>
    /// When the grant was consumed (resolved), or null if it was still usable when it was looked up.
    /// </summary>
    public DateTimeOffset? Resolved { get; set; }
}
