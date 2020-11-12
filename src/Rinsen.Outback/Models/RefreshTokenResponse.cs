using System.Text.Json.Serialization;

namespace Rinsen.Outback.Models
{
    public class RefreshTokenResponse : TokenResponse
    {
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
    }
}
