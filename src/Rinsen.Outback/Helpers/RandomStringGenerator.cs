using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;

namespace Rinsen.Outback.Helpers;

public class RandomStringGenerator
{
    private readonly RandomNumberGenerator CryptoRandom = RandomNumberGenerator.Create();

    public string GetRandomString(int length)
    {
        var bytes = new byte[length];

        CryptoRandom.GetBytes(bytes);

        return Base64UrlTextEncoder.Encode(bytes);
    }
}
