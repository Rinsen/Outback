using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Rinsen.Outback.Accessors;
using SampleServer;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// OAuth 2.0 Device Authorization Grant 
    /// rfc8628
    /// </summary>
    public class DeviceAuthorizationFlow
    {
        /// <summary>
        /// Testing basic flow described in Figure 1 in rfc8628
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task TestingBasicFlow()
        {
            // Arrange
            var application = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    // ... Configure test services
                });

            // +----------+                                +----------------+
            // |          |>---(A)-- Client Identifier --->|                |
            // |          |                                |                |
            // |          |<---(B)-- Device Code,      ---<|                |
            // |          |          User Code,            |                |
            // |  Device  |          & Verification URI    |                |
            // |  Client  |                                |                |
            // |          |  [polling]                     |                |
            // |          |>---(E)-- Device Code       --->|                |
            // |          |          & Client Identifier   |                |
            // |          |                                |  Authorization |
            // |          |<---(F)-- Access Token      ---<|     Server     |
            // +----------+   (& Optional Refresh Token)   |                |
            //       v                                     |                |
            //       :                                     |                |
            //      (C) User Code & Verification URI       |                |
            //       :                                     |                |
            //       v                                     |                |
            // +----------+                                |                |
            // | End User |                                |                |
            // |    at    |<---(D)-- End user reviews  --->|                |
            // |  Browser |          authorization request |                |
            // +----------+                                +----------------+

            // (A) The client requests access from the authorization server and
            // includes its client identifier in the request.

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", "DeviceAuthorizationClientId")
            });

            var client = application.CreateClient();
            var grantAccessor = application.Services.GetService<IGrantAccessor>();
            var response = await client.PostAsync("device_authorization", formContent);

            // (B) The authorization server issues a device code and an end - user code and provides the end - user verification URI.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var device_authorization_Response = JsonConvert.DeserializeObject<device_authorization_Response>(content);

            //Assert.Equal("{\"error\":\"invalid_request\"}", response2);

            // (C) The client instructs the end user to use a user agent on another
            // device and visit the provided end-user verification URI.  The
            // client provides the user with the end-user code to enter in
            // order to review the authorization request.

            // (D) The authorization server authenticates the end user(via the
            // user agent), and prompts the user to input the user code
            // provided by the device client.  The authorization server
            // validates the user code provided by the user, and prompts the
            // user to accept or decline the request.

            // (E) While the end user reviews the client's request (step D), the
            // client repeatedly polls the authorization server to find out if
            // the user completed the user authorization step.  The client
            // includes the device code and its client identifier.

            // (F) The authorization server validates the device code provided by
            // the client and responds with the access token if the client is
            // granted access, an error if they are denied access, or an
            // indication that the client should continue to poll.

        }


        public class device_authorization_Response
        {
            public string device_code { get; set; }
            public string user_code { get; set; }
            public string verification_uri { get; set; }
            public string verification_uri_complete { get; set; }
            public int expires_in { get; set; }
            public int interval { get; set; }
        }


    }
}
