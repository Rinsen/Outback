using System.Text.Json.Serialization;

namespace Rinsen.Outback.Models;

public class AccessTokenWithIdentityTokenAndRefreshTokenResponse : AccessTokenResponse
{
    /// <summary>
    /// id_token
    /// </summary>
    [JsonPropertyName("id_token")]
    public string IdentityToken { get; set; } = string.Empty;

    /// <summary>
    /// refresh_token
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;
}
