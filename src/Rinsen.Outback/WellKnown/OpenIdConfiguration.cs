using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Rinsen.Outback.WellKnown
{
    public class OpenIdConfiguration
    {
        [JsonPropertyName("issuer")]
        public string Issuer { get; set; }

        [JsonPropertyName("jwks_uri")]
        public string JwksUri { get; set; }

        [JsonPropertyName("authorization_endpoint")]
        public string AuthorizationEndpoint { get; set; }

        [JsonPropertyName("token_endpoint")]
        public string TokenEndpoint { get; set; }

        //public string userinfo_endpoint { get; set; }
        //public string end_session_endpoint { get; set; }
        //public string check_session_iframe { get; set; }
        //public string revocation_endpoint { get; set; }
        //public string introspection_endpoint { get; set; }
        //public string device_authorization_endpoint { get; set; }

        [JsonPropertyName("frontchannel_logout_supported")]
        public bool FrontchannelLogoutSupported { get; set; }

        [JsonPropertyName("frontchannel_logout_session_supported")]
        public bool FrontchannelLogoutSessionSupported { get; set; }

        [JsonPropertyName("backchannel_logout_supported")]
        public bool BackchannelLogoutSupported { get; set; }

        [JsonPropertyName("backchannel_logout_session_supported")]
        public bool BackchannelLogoutSessionSupported { get; set; }

        [JsonPropertyName("scopes_supported")]
        public List<string> ScopesSupported { get; set; }

        [JsonPropertyName("claims_supported")]
        public List<object> ClaimsSupported { get; set; }

        [JsonPropertyName("grant_types_supported")]
        public List<string> GrantTypesSupported { get; set; }

        [JsonPropertyName("response_types_supported")]
        public List<string> ResponseTypesSupported { get; set; }

        [JsonPropertyName("token_endpoint_auth_methods_supported")]
        public List<string> TokenEndpointAuthMethodsSupported { get; set; }

        [JsonPropertyName("subject_types_supported")]
        public List<string> SubjectTypesSupported { get; set; }

        [JsonPropertyName("code_challenge_methods_supported")]
        public List<string> CodeChallengeMethodsSupported { get; set; }

        [JsonPropertyName("request_parameter_supported")]
        public bool RequestParameterSupported { get; set; }
    }
}
