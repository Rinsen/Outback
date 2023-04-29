using System.Text.Json.Serialization;

namespace Rinsen.Outback.Models;

public class AccessTokenWithIdentityTokenResponse : AccessTokenResponse
{

    [JsonPropertyName("id_token")]
    public string IdentityToken { get; set; } = string.Empty;
}
