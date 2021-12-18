using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Rinsen.Outback.WellKnown;
using Xunit;

namespace Rinsen.Outback.Tests
{
    public class WellKnownEndpoint
    {
        [Fact]
        public async Task MustReturnInformationThatMakesSense()
        {
            // Arrange
            var application = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    // ... Configure test services
                });

            var client = application.CreateClient();
            
            // Act
            var response = await client.GetAsync(".well-known/openid-configuration");

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299

            var result = await response.Content.ReadFromJsonAsync<OpenIdConfiguration>();

            Assert.NotNull(result);
            Assert.Equal("https://localhost", result?.Issuer);
            
        }
    }
}
