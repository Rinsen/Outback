using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using Rinsen.Outback.Tests.Helpers;
using SampleServer;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// OAuth 2.1
    /// 4.2 Client Credentials Grant
    /// </summary>
    public class MachineToMachine
    {
        [Fact]
        public async Task WhenUsingClientCredentialsInPostFormGetAccessToken()
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

            var client = application.CreateClient();

            // Act
            var response = await client.PostAsync("oauth/token", formContent);

            // Assert
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();

            Assert.NotNull(result);
            Assert.Equal(100, result?.ExpiresIn);
            Assert.Equal("Bearer", result?.TokenType);
        }

        [Fact]
        public async Task WhenUsingClientCredentialsInBasicAuthHeaderGetAccessToken()
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
            });

            var encodedClientIdAndSecret = Base64UrlEncoder.Encode("MachineToMachineClientId:pwd");

            var client = application.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedClientIdAndSecret);

            // Act
            var response = await client.PostAsync("oauth/token", formContent);

            // Assert
            response.EnsureSuccessStatusCode();
            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

            if (tokenResponse == null)
            {
                throw new System.Exception("Null token response");
            }

            var accessToken = await JwtValidationHelper.ValidateToken(client, tokenResponse.AccessToken, "MessagingServer", "https://localhost");

            Assert.Equal(100, tokenResponse.ExpiresIn);
            Assert.Equal("Bearer", tokenResponse.TokenType);
            Assert.Contains(accessToken.Claims, m => m.Key == "extra_claim" && (string)m.Value == "with value");
        }
    }
}
