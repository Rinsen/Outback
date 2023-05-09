using System;

namespace Rinsen.Outback.Grants
{
    public class DeviceAuthorizationGrant
    {
        /// <summary>
        /// The client that is Granted access for the Subject.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// The user associated to this Grant.
        /// </summary>
        public string? SubjectId { get; set; } = string.Empty;

        /// <summary>
        /// The device verification code.
        /// </summary>
        public string DeviceCode { get; set; } = string.Empty;

        /// <summary>
        /// The end-user verification code.
        /// </summary>
        public string UserCode { get; set; } = string.Empty;

        /// <summary>
        /// The lifetime of the "DeviceCode" and "UserCode".
        /// <para>OPTIONAL.</para>
        /// </summary>
        public DateTimeOffset UserCodeExpiration { get; set; }

        /// <summary>
        /// The minimum amount of time in seconds that the client
        /// SHOULD wait between polling requests to the token endpoint.If no
        /// value is provided, clients MUST use 5 as the default.
        /// <para>OPTIONAL.</para>
        /// </summary>
        public int Interval { get; set; }

    }
}
