using System.Text.Json.Serialization;

namespace Rinsen.Outback.Models
{
    public class IdentityTokenResponse : AccessTokenResponse
    {

        [JsonPropertyName("id_token")]
        public string IdentityToken { get; set; }
    }
}
