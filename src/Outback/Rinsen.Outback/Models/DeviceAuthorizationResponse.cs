using System.Text.Json.Serialization;

namespace Rinsen.Outback.Models
{
    public class DeviceAuthorizationResponse
    {
        /// <summary>
        /// The device verification code.
        /// <para>REQUIRED. (device_code)</para>
        /// </summary>
        [JsonPropertyName("device_code")]
        public string DeviceCode { get; set; } = string.Empty;

        /// <summary>
        /// The end-user verification code.
        /// <para>REQUIRED. (user_code)</para>
        /// </summary>
        [JsonPropertyName("user_code")]
        public string UserCode { get; set; } = string.Empty;

        /// <summary>
        /// The end-user verification URI on the authorization server.
        /// The URI should be short and easy to remember as end users will be asked to manually type it into their user agent.
        /// <para>REQUIRED. (verification_uri)</para>
        /// </summary>
        [JsonPropertyName("verification_uri")]
        public string VerificationUri { get; set; } = string.Empty;

        /// <summary>
        /// A verification URI that includes the "user_code" (or other information with the same function as the "user_code"), which is designed for non-textual transmission.
        /// <para>OPTIONAL. (verification_uri_complete)</para>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("verification_uri_complete")]
        public string VerificationUriComplete { get; set; } = string.Empty;

        /// <summary>
        /// The lifetime in seconds of the "device_code" and "user_code" for completion by subject.
        /// <para>REQUIRED. (expires_in)</para>
        /// </summary>
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// The minimum amount of time in seconds that the client SHOULD wait between polling requests to the token endpoint. If no value is provided, clients MUST use 5 as the default.
        /// <para>OPTIONAL. (interval)</para>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("interval")]
        public int? Interval { get; set; } = default;

    }
}
