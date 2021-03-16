using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Rinsen.Outback
{
    public static class HashHelper
    {
        public static string GetSha256Hash(string secret)
        {
            using var sha256 = SHA256.Create();

            return WebEncoders.Base64UrlEncode(sha256.ComputeHash(Encoding.UTF8.GetBytes(secret)));
        }
    }
}
