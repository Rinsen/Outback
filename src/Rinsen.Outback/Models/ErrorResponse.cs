using System.Text.Json.Serialization;

namespace Rinsen.Outback.Models
{
    public class ErrorResponse
    {
        /// <summary>
        /// REQUIRED.  
        /// A single ASCII [USASCII] error code from ErrorResponses:
        /// </summary>
        [JsonPropertyName("error")]
        public string Error { get; set; }

        /// <summary>
        /// OPTIONAL.  
        /// Human-readable ASCII [USASCII] text
        /// providing additional information, used to assist the client
        /// developer in understanding the error that occurred.Values for
        /// the error_description parameter MUST NOT include characters
        /// outside the set %x20-21 / %x23-5B / %x5D-7E.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; } = null;

        /// <summary>
        /// OPTIONAL.  
        /// A URI identifying a human-readable web page
        /// with information about the error, used to provide the client
        /// developer with additional information about the error.Values for
        /// the error_uri parameter MUST conform to the URI-reference syntax
        /// and thus MUST NOT include characters outside the set %x21 / %x23-5B / %x5D-7E.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("error_uri")]
        public string ErrorUri { get; set; } = null;

    }

    public static class ErrorResponses
    {
        /// <summary>
        /// The request is missing a required parameter,
        /// includes an unsupported parameter value(other than grant
        /// type), repeats a parameter, includes multiple credentials,
        /// utilizes more than one mechanism for authenticating the client,
        /// contains a code_verifier although no code_challenge was sent in
        /// the authorization request, or is otherwise malformed.
        /// </summary>
        public const string InvalidRequest = "invalid_request";

        /// <summary>
        /// Client authentication failed (e.g., unknown
        /// client, no client authentication included, or unsupported
        /// authentication method).  The authorization server MAY return an
        /// HTTP 401 (Unauthorized) status code to indicate which HTTP
        /// authentication schemes are supported.If the client attempted
        /// to authenticate via the Authorization request header field, the
        /// authorization server MUST respond with an HTTP 401
        /// (Unauthorized) status code and include the WWW-Authenticate
        /// response header field matching the authentication scheme used
        /// by the client.
        /// </summary>
        public const string InvalidClient = "invalid_client";

        /// <summary>
        /// The provided authorization grant (e.g.,
        /// authorization code, resource owner credentials) or refresh
        /// token is invalid, expired, revoked, does not match the redirect
        /// URI used in the authorization request, or was issued to another
        /// client.
        /// </summary>
        public const string InvalidGrant = "invalid_grant";

        /// <summary>
        /// The authenticated client is not authorized
        /// to use this authorization grant type.
        /// </summary>
        public const string UnauthorizedClient = "unauthorized_client";

        /// <summary>
        /// The authorization grant type is not
        /// supported by the authorization server.
        /// </summary>
        public const string UnsupportedGrantType = "unsupported_grant_type";

        /// <summary>
        /// The requested scope is invalid, unknown,
        /// malformed, or exceeds the scope granted by the resource owner.
        /// </summary>
        public const string InvalidScope = "invalid_scope";

    }
}
