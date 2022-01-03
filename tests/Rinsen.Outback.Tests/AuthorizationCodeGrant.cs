using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Rinsen.Outback.Tests.Helpers;
using SampleServer;
using Xunit;

namespace Rinsen.Outback.Tests
{
    public class AuthorizationCodeGrant
    {
        /// <summary>
        /// 4.1. Authorization Code Grant
        /// The authorization code grant type is used to obtain both access tokens and refresh tokens.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task VerifyBasicPkceCodeFlow()
        {
            // Arrange
            var application = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
                    });
                });

            var encodedClientIdAndSecret = Base64UrlEncoder.Encode("PKCEWebClientId:pwd");
            using var sha256 = SHA256.Create();
            var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes("abcdefghijklmnopqrstuvqyz"));
            var codeChallenge = WebEncoders.Base64UrlEncode(challengeBytes);
            var uri = "connect/authorize";
            uri = QueryHelpers.AddQueryString(uri, "response_type", "code");
            uri = QueryHelpers.AddQueryString(uri, "client_id", "PKCEWebClientId");
            uri = QueryHelpers.AddQueryString(uri, "code_challenge", codeChallenge);
            uri = QueryHelpers.AddQueryString(uri, "scope", "openid");

            var client = application.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }); ;

            // Act
            var response = await client.GetAsync(uri);

            var result = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

        }
    }
}
