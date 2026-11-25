namespace Rinsen.Outback.Helpers;

/// <summary>
/// SEC-020: policy for the anonymous-registration invite code. The code is a
/// shared secret that gates public self-registration, so it must be a strong
/// random value of at least 128 bits. 128 bits is canonically 16 random bytes —
/// e.g. 32 hex characters from <c>openssl rand -hex 16</c> — and a configured
/// code shorter than that is treated as too weak: self-registration fails
/// closed instead of being gated by a guessable value.
/// </summary>
public static class InviteCodePolicy
{
    /// <summary>
    /// Minimum configured-code length in characters (128 bits for hex).
    /// </summary>
    public const int MinimumLength = 32;

    /// <summary>
    /// <c>true</c> when the configured code is present and at least
    /// <see cref="MinimumLength"/> characters long.
    /// </summary>
    public static bool IsStrong(string? configuredCode)
    {
        return configuredCode is not null && configuredCode.Length >= MinimumLength;
    }
}
