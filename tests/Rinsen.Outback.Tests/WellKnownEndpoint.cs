using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
            var client = await CreateClientAsync();

            // Act
            var openIdConfigurationModel = await client.GetOpenIdConfiguration();


            Assert.NotNull(openIdConfigurationModel);
            // the default test client speaks http, so the issuer must match the request scheme
            Assert.Equal("http://localhost", openIdConfigurationModel?.Issuer);
            
        }

        [Fact]
        public async Task Issuer_MatchesRequestScheme_OverHttp()
        {
            var client = await CreateClientAsync(new Uri("http://localhost"));

            var openIdConfigurationModel = await client.GetOpenIdConfiguration();

            Assert.Equal("http://localhost", openIdConfigurationModel.Issuer);
        }

        [Fact]
        public async Task Issuer_MatchesRequestScheme_OverHttps()
        {
            var client = await CreateClientAsync(new Uri("https://localhost"));

            var openIdConfigurationModel = await client.GetOpenIdConfiguration();

            Assert.Equal("https://localhost", openIdConfigurationModel.Issuer);
        }

        [Fact]
        public async Task DiscoveryUrls_UseTheSchemeOfTheRequest()
        {
            foreach (var scheme in new[] { "http", "https" })
            {
                var client = await CreateClientAsync(new Uri($"{scheme}://localhost"));

                var openIdConfigurationModel = await client.GetOpenIdConfiguration();

                var baseUrl = $"{scheme}://localhost";

                Assert.Equal(baseUrl, openIdConfigurationModel.Issuer);
                Assert.StartsWith(baseUrl, openIdConfigurationModel.JwksUri);
                Assert.StartsWith(baseUrl, openIdConfigurationModel.AuthorizationEndpoint);
                Assert.StartsWith(baseUrl, openIdConfigurationModel.TokenEndpoint);
                Assert.StartsWith(baseUrl, openIdConfigurationModel.UserInfoEndpoint);
                Assert.StartsWith(baseUrl, openIdConfigurationModel.DeviceAuthorizationEndpoint);
            }
        }

        [Fact]
        public async Task ResponseTypesSupported_ContainsCode()
        {
            var client = await CreateClientAsync();

            var openIdConfigurationModel = await client.GetOpenIdConfiguration();

            Assert.Equal(new List<string> { "code" }, openIdConfigurationModel.ResponseTypesSupported);
        }

        [Fact]
        public async Task IdTokenSigningAlgValuesSupported_ContainsEs256()
        {
            var client = await CreateClientAsync();

            var openIdConfigurationModel = await client.GetOpenIdConfiguration();

            Assert.Equal(new List<string> { "ES256" }, openIdConfigurationModel.IdTokenSigningAlgValuesSupported);
        }

        [Fact]
        public async Task UserInfoEndpoint_IsPresentAndReachable()
        {
            var client = await CreateClientAsync();

            var openIdConfigurationModel = await client.GetOpenIdConfiguration();

            Assert.False(string.IsNullOrEmpty(openIdConfigurationModel.UserInfoEndpoint));

            // unauthenticated, so the endpoint must answer (401) rather than be missing (404)
            var response = await client.GetAsync("oauth/userinfo");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SubjectTypesSupported_ContainsPublic()
        {
            var client = await CreateClientAsync();

            var openIdConfigurationModel = await client.GetOpenIdConfiguration();

            Assert.Contains("public", openIdConfigurationModel.SubjectTypesSupported);
        }

        [Fact]
        public async Task GrantTypesSupported_ContainsNoDuplicates()
        {
            var client = await CreateClientAsync();

            var openIdConfigurationModel = await client.GetOpenIdConfiguration();

            Assert.Contains("authorization_code", openIdConfigurationModel.GrantTypesSupported);
            Assert.Equal(openIdConfigurationModel.GrantTypesSupported.Distinct().Count(), openIdConfigurationModel.GrantTypesSupported.Count());
        }

        private static async Task<HttpClient> CreateClientAsync(Uri baseAddress = null)
        {
            var application = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    // ... Configure test services
                });

            if (baseAddress == null)
            {
                return application.CreateClient();
            }

            return application.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = baseAddress });
        }
    }
}
