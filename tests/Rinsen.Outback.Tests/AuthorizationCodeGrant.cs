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
        public async Task VerifyBasicAuthorizationCodeGrantFlow()
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
            var codeVerifier = "abcdefghijklmnopqrstuvqyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~0123456789";
            var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            var codeChallenge = WebEncoders.Base64UrlEncode(challengeBytes);
            var uri = "connect/authorize";
            uri = QueryHelpers.AddQueryString(uri, "response_type", "code");
            uri = QueryHelpers.AddQueryString(uri, "client_id", "PKCEWebClientId");
            uri = QueryHelpers.AddQueryString(uri, "code_challenge", codeChallenge);
            uri = QueryHelpers.AddQueryString(uri, "scope", "openid");

            var client = application.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }); ;

            // Act
            var authorizeGetResponse = await client.GetAsync(uri);
            var authorizeGetResponseContent = await authorizeGetResponse.Content.ReadAsStringAsync();
            var locationUri = authorizeGetResponse.Headers.Location;
            var queryParameters = QueryHelpers.ParseQuery(locationUri?.Query);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedClientIdAndSecret);

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", queryParameters["code"][0]),
                new KeyValuePair<string, string>("code_verifier", codeVerifier),
            });
            var response = await client.PostAsync("connect/token", formContent);
            var tokenResponse = await client.PostAsync("connect/token", formContent);
            var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Redirect, authorizeGetResponse.StatusCode);
            Assert.Equal(0, authorizeGetResponseContent.Length);
            Assert.NotNull(locationUri);
            Assert.Equal("my.domain", locationUri?.Host);
            Assert.Equal("https", locationUri?.Scheme);
            Assert.Equal("/signin-oidc", locationUri?.AbsolutePath);
            Assert.True(queryParameters.ContainsKey("code"));
            Assert.Equal(20, queryParameters["code"][0].Length);
            Assert.True(queryParameters.ContainsKey("scope"));
            Assert.Equal("openid", queryParameters["scope"][0]);

            //Assert.Equal(System.Net.HttpStatusCode.Redirect, );

        }
    }
}
