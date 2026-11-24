using System;
using System.Security.Cryptography;
using System.Text;

namespace Rinsen.Outback.Helpers;

/// <summary>
/// Constant-time comparisons for secrets (client secrets, password hashes, invite
/// codes, code challenges, etc.) to avoid timing side-channels. The comparison is
/// performed with <see cref="CryptographicOperations.FixedTimeEquals"/>, which is
/// length-checked and does not short-circuit on the first differing byte, so the
/// time taken does not reveal how many leading characters/bytes matched.
/// </summary>
public static class SecretComparer
{
    /// <summary>
    /// Compares two secret strings in constant time.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>
    /// <c>true</c> if both values are non-null and their UTF-8 encodings are equal;
    /// otherwise <c>false</c> (including when either value is null or their lengths
    /// differ).
    /// </returns>
    public static bool FixedTimeEquals(string? left, string? right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left).AsSpan(),
            Encoding.UTF8.GetBytes(right).AsSpan());
    }

    /// <summary>
    /// Compares two secret byte arrays in constant time.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>
    /// <c>true</c> if both values are non-null and have the same length and contents;
    /// otherwise <c>false</c> (including when either value is null or their lengths
    /// differ).
    /// </returns>
    public static bool FixedTimeEquals(byte[]? left, byte[]? right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(left.AsSpan(), right.AsSpan());
    }
}
