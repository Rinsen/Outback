using System.Text.Json.Serialization;

namespace Rinsen.Outback.Models
{
    public class AccessTokenResponse
    {
        /// <summary>
        /// access_token
        /// </summary>
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// token_type
        /// </summary>
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        /// <summary>
        /// expires_in
        /// </summary>
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
