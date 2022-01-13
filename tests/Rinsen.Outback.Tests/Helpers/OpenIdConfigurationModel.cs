using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Rinsen.Outback.Tests.Helpers
{
    public class OpenIdConfigurationModel
    {
        [JsonPropertyName("issuer")]
        public string Issuer { get; set; } = string.Empty;

        [JsonPropertyName("jwks_uri")]
        public string JwksUri { get; set; } = string.Empty;

        [JsonPropertyName("authorization_endpoint")]
        public string AuthorizationEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("token_endpoint")]
        public string TokenEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("frontchannel_logout_supported")]
        public bool FrontchannelLogoutSupported { get; set; }

        [JsonPropertyName("frontchannel_logout_session_supported")]
        public bool FrontchannelLogoutSessionSupported { get; set; }

        [JsonPropertyName("backchannel_logout_supported")]
        public bool BackchannelLogoutSupported { get; set; }

        [JsonPropertyName("backchannel_logout_session_supported")]
        public bool BackchannelLogoutSessionSupported { get; set; }

        [JsonPropertyName("scopes_supported")]
        public List<string> ScopesSupported { get; set; } = new List<string>();

        [JsonPropertyName("claims_supported")]
        public List<string> ClaimsSupported { get; set; } = new List<string>();

        [JsonPropertyName("grant_types_supported")]
        public List<string> GrantTypesSupported { get; set; } = new List<string>();

        [JsonPropertyName("response_types_supported")]
        public List<string> ResponseTypesSupported { get; set; } = new List<string>();

        [JsonPropertyName("token_endpoint_auth_methods_supported")]
        public List<string> TokenEndpointAuthMethodsSupported { get; set; } = new List<string>();

        [JsonPropertyName("subject_types_supported")]
        public List<string> SubjectTypesSupported { get; set; } = new List<string>();

        [JsonPropertyName("code_challenge_methods_supported")]
        public List<string> CodeChallengeMethodsSupported { get; set; } = new List<string>();

        [JsonPropertyName("request_parameter_supported")]
        public bool RequestParameterSupported { get; set; }

    }
}
