using System;

namespace Rinsen.IdentityProvider;

/// <summary>
/// A user as seen from the admin API: an identity combined with its 1:1 local
/// account. Never contains secrets (password hash/salt, TOTP secret).
/// </summary>
public class User
{
    public Guid IdentityId { get; set; }

    public string GivenName { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string LoginId { get; set; } = string.Empty;

    public bool HasLocalAccount { get; set; }

    public bool IsAdministrator { get; set; }

    public bool TwoFactorTotpEnabled { get; set; }

    public bool IsDisabled { get; set; }

    public DateTimeOffset Created { get; set; }

    public DateTimeOffset Updated { get; set; }
}
