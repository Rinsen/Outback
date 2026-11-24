using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Rinsen.Outback.Helpers;

public static class HashHelper
{
    /// <summary>
    /// Prefix of the salted client-secret format:
    /// <c>PBKDF2-SHA256$&lt;iterations&gt;$&lt;base64url salt&gt;$&lt;base64url hash&gt;</c>.
    /// The salt is embedded in the stored value so it can be persisted alongside the hash.
    /// </summary>
    public const string ClientSecretFormat = "PBKDF2-SHA256";

    /// <summary>
    /// OWASP minimum iteration count for PBKDF2-HMAC-SHA256.
    /// </summary>
    public const int ClientSecretIterationCount = 600_000;

    private const int SaltSize = 16;
    private const int HashSize = 32;

    /// <summary>
    /// Legacy unsalted SHA-256 hash. Kept only to verify secrets that were stored
    /// before SEC-013; new secrets must be stored with <see cref="HashClientSecret"/>.
    /// </summary>
    public static string GetSha256Hash(string secret)
    {
        using var sha256 = SHA256.Create();

        return WebEncoders.Base64UrlEncode(sha256.ComputeHash(Encoding.UTF8.GetBytes(secret)));
    }

    /// <summary>
    /// Hashes a client secret with a fresh random salt using PBKDF2-HMAC-SHA256.
    /// The salt is embedded in the returned value, which is what gets stored.
    /// </summary>
    public static string HashClientSecret(string secret)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(secret),
            salt,
            ClientSecretIterationCount,
            HashAlgorithmName.SHA256,
            HashSize);

        return $"{ClientSecretFormat}${ClientSecretIterationCount}${WebEncoders.Base64UrlEncode(salt)}${WebEncoders.Base64UrlEncode(hash)}";
    }

    /// <summary>
    /// Verifies a plaintext client secret against a stored value, supporting both the
    /// salted PBKDF2 format and the legacy unsalted SHA-256 format. The comparison
    /// is constant-time; malformed stored values fail closed.
    /// </summary>
    public static bool VerifyClientSecret(string secret, string stored)
    {
        if (secret == null || stored == null)
        {
            return false;
        }

        if (stored.StartsWith(ClientSecretFormat + "$", StringComparison.Ordinal))
        {
            var parts = stored.Split('$');

            if (parts.Length != 4 || parts[0] != ClientSecretFormat)
            {
                return false;
            }

            if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
            {
                return false;
            }

            byte[] salt;
            byte[] expected;

            try
            {
                salt = WebEncoders.Base64UrlDecode(parts[2]);
                expected = WebEncoders.Base64UrlDecode(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            if (salt.Length == 0 || expected.Length == 0)
            {
                return false;
            }

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(secret),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);

            return SecretComparer.FixedTimeEquals(actual, expected);
        }

        // Legacy unsalted SHA-256 format.
        return SecretComparer.FixedTimeEquals(GetSha256Hash(secret), stored);
    }

    /// <summary>
    /// True when the stored value is in the legacy unsalted SHA-256 format and should
    /// be re-hashed with <see cref="HashClientSecret"/> after a successful verification.
    /// </summary>
    public static bool IsLegacyClientSecret(string stored)
    {
        return stored != null && !stored.StartsWith(ClientSecretFormat + "$", StringComparison.Ordinal);
    }
}
