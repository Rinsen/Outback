using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using SampleServer;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// OAuth 2.1
    /// 4.2 Client Credentials Grant
    /// </summary>
    public class ClientAuthentication
    {


        /// <summary>
        /// 2.4.1.  Client Secret
        /// 3.2.3.1.  Error Response
        /// The client MUST NOT use more than one authentication method in each request to prevent a conflict of which authentication mechanism is authoritative for the request.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task WhenUsingClientCredentialsInBasicAuthHeaderAndFormNoTokenIsIssued()
        {
            // Arrange
            var application = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    // ... Configure test services
                });

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", "MachineToMachineClientId"),
                new KeyValuePair<string, string>("client_secret", "pwd")
            });

            var encodedClientIdAndSecret = Base64UrlEncoder.Encode("MachineToMachineClientId:pwd");

            var client = application.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedClientIdAndSecret);

            // Act
            var response = await client.PostAsync("connect/token", formContent);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            var result = await response.Content.ReadAsStringAsync();

        }
    }
}
