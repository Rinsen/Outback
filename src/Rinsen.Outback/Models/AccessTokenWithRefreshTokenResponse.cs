using System.Text.Json.Serialization;

namespace Rinsen.Outback.Models
{
    public class AccessTokenWithRefreshTokenResponse : AccessTokenResponse
    {
        /// <summary>
        /// refresh_token
        /// </summary>
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
    }
}
