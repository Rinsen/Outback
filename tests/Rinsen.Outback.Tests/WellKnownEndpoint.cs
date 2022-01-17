using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Rinsen.Outback.Tests.Helpers;
using SampleServer;
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
            var openIdConfigurationModel = await client.GetOpenIdConfiguration();


            Assert.NotNull(openIdConfigurationModel);
            Assert.Equal("https://localhost", openIdConfigurationModel?.Issuer);
            
        }
    }
}
