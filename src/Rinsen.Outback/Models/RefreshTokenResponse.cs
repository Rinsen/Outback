using System.Text.Json.Serialization;

namespace Rinsen.Outback.Models
{
    public class RefreshTokenResponse : AccessTokenResponse
    {
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
    }
}
