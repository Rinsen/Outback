using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Rinsen.Outback
{
    public class EllipticCurveJsonWebKeyModel
    {
        [JsonPropertyName("kty")]
        public string KeyType { get { return "EC"; } }

        [JsonPropertyName("use")]
        public string PublicKeyUse { get { return "sig"; } }

        [JsonPropertyName("kid")]
        public string KeyId { get; set; }

        [JsonPropertyName("x")]
        public string X { get; set; }

        [JsonPropertyName("y")]
        public string Y { get; set; }

        [JsonPropertyName("crv")]
        public string Curve { get; set; }

        [JsonPropertyName("alg")]
        public string SigningAlgorithm { get; set; }

    }
}
