using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Tests.Helpers;
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
            var grantAccessor = application.Services.GetRequiredService<IGrantAccessor>();
            var response = await client.PostAsync("device_authorization", formContent);

            // (B) The authorization server issues a device code and an end - user code and provides the end - user verification URI.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var device_authorization_Response = JsonConvert.DeserializeObject<device_authorization_Response>(content);
            
            // (C) The client instructs the end user to use a user agent on another
            // device and visit the provided end-user verification URI.  The
            // client provides the user with the end-user code to enter in
            // order to review the authorization request.

            // Not in scope of this project

            // (D) The authorization server authenticates the end user(via the
            // user agent), and prompts the user to input the user code
            // provided by the device client.  The authorization server
            // validates the user code provided by the user, and prompts the
            // user to accept or decline the request.

            var grant = await grantAccessor.GetDeviceAuthorizationGrantAsync(device_authorization_Response.device_code);
            Assert.Equal(device_authorization_Response.user_code, grant.UserCode);

            // (E) While the end user reviews the client's request (step D), the
            // client repeatedly polls the authorization server to find out if
            // the user completed the user authorization step.  The client
            // includes the device code and its client identifier.

            var tokenRequestFormContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
                new KeyValuePair<string, string>("device_code", device_authorization_Response.device_code),
                new KeyValuePair<string, string>("client_id", "DeviceAuthorizationClientId"),
            });

            var notAcceptedTokenRequest = await client.PostAsync("connect/token", tokenRequestFormContent);

            var error_Response = await notAcceptedTokenRequest.Content.ReadFromJsonAsync<error_response>();
            Assert.Equal("authorization_pending", error_Response.error);

            // User accepts device client request
            grant.SubjectId = "123456789";

            // (F) The authorization server validates the device code provided by
            // the client and responds with the access token if the client is
            // granted access, an error if they are denied access, or an
            // indication that the client should continue to poll.

            var tokenResponseAfterAccept = await client.PostAsync("connect/token", tokenRequestFormContent);

            var tokenResponse = await tokenResponseAfterAccept.Content.ReadFromJsonAsync<token_response>();

            Assert.NotNull(tokenResponse);

            var accessToken = await JwtValidationHelper.ValidateToken(client, tokenResponse.access_token, "OutbackAS", "https://localhost");

            Assert.True(accessToken.IsValid);
            Assert.Equal(8, accessToken.Claims.Count());
            Assert.Equal("DeviceAuthorizationClientId", accessToken.Claims.Single(m => m.Key == "client_id").Value);
            Assert.Equal("openid profile", accessToken.Claims.Single(m => m.Key == "scope").Value);
            Assert.Equal("123456789", accessToken.Claims.Single(m => m.Key == "sub").Value);
            Assert.Equal("https://localhost", accessToken.Claims.Single(m => m.Key == "iss").Value);
            var audienceClaim = accessToken.Claims.Single(m => m.Key == "aud");
            Assert.Equal("OutbackAS", audienceClaim.Value);
            Assert.Single(accessToken.Claims, m => m.Key == "exp");
            Assert.Single(accessToken.Claims, m => m.Key == "nbf");
            Assert.Single(accessToken.Claims, m => m.Key == "iat");
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


        public class error_response
        {
            public string error { get; set; }
        }


        public class token_response
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public int expires_in { get; set; }
        }




    }
}
