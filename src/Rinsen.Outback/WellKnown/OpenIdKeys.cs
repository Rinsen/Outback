using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Rinsen.Outback.WellKnown
{
    public class EllipticCurveJsonWebKeyModelKeys
    {
        [JsonPropertyName("keys")]
        public List<EllipticCurveJsonWebKeyModel> Keys { get; set; }
    }
}
