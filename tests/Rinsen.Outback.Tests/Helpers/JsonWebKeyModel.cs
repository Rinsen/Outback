using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Rinsen.Outback.Tests.Helpers
{
    public class JsonWebKeyModel
    {
        [JsonPropertyName("kty")]
        public string KeyType { get; set; } = string.Empty;

        [JsonPropertyName("use")]
        public string PublicKeyUse { get; set; } = string.Empty;

        [JsonPropertyName("kid")]
        public string KeyId { get; set; } = string.Empty;

        [JsonPropertyName("x")]
        public string X { get; set; } = string.Empty;

        [JsonPropertyName("y")]
        public string Y { get; set; } = string.Empty;

        [JsonPropertyName("crv")]
        public string Curve { get; set; } = string.Empty;

        [JsonPropertyName("alg")]
        public string SigningAlgorithm { get; set; } = string.Empty;
    }

    public class JsonWebKeyModelKeys
    {
        [JsonPropertyName("keys")]
        public List<JsonWebKeyModel> Keys { get; set; } = new List<JsonWebKeyModel>();
    }
}
