using System.Text.Json.Serialization;

namespace Rinsen.Outback.Models;

public class AccessTokenResponse
{
    /// <summary>
    /// access_token
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// token_type
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    /// <summary>
    /// expires_in
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; } = 1;
}
