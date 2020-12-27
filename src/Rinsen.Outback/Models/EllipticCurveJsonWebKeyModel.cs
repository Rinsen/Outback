using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Rinsen.Outback.Models
{
    public class EllipticCurveJsonWebKeyModelKeys
    {
        [JsonPropertyName("keys")]
        public List<EllipticCurveJsonWebKeyModel> Keys { get; set; }
    }

    public class EllipticCurveJsonWebKeyModel
    {
        public EllipticCurveJsonWebKeyModel(string keyId, string x, string y, string curve, string signingAlgorithm)
        {
            KeyType = "EC";
            PublicKeyUse = "sig";
            KeyId = keyId;
            X = x;
            Y = y;
            Curve = curve;
            SigningAlgorithm = signingAlgorithm;
        }

        [JsonPropertyName("kty")]
        public string KeyType { get; }

        [JsonPropertyName("use")]
        public string PublicKeyUse { get; }

        [JsonPropertyName("kid")]
        public string KeyId { get; }

        [JsonPropertyName("x")]
        public string X { get; }

        [JsonPropertyName("y")]
        public string Y { get; }

        [JsonPropertyName("crv")]
        public string Curve { get; }

        [JsonPropertyName("alg")]
        public string SigningAlgorithm { get; }

    }
}
