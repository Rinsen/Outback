using System.Text.Json.Serialization;

namespace Rinsen.Outback.Models;

public class AccessTokenResponse
{
    /// <summary>
    /// Gets or sets the access token used for authentication or authorization.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of token returned by the authentication process.
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identity token associated with the current user or session.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("id_token")]
    public string? IdentityToken { get; set; } = null;

    /// <summary>
    /// Gets or sets the refresh token used to obtain a new access token when the current token expires.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; } = null;

    /// <summary>
    /// Gets or sets the duration, in seconds, until the token expires.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; } = 1;
}
