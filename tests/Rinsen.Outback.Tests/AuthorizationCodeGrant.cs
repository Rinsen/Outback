using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
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

            var client = application.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }); ;

            var clientId = "PKCEWebClientId";
            var codeVerifier = "abcdefghijklmnopqrstuvqyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~0123456789";
            var code = await GetCodeAsync(codeVerifier, client, clientId);

            var tokenResponse = await GetTokenResponse(client, codeVerifier, code);

            if (tokenResponse == null)
                throw new Exception("Token response is null");
            
            var accessToken = await JwtValidationHelper.ValidateToken(client, tokenResponse.AccessToken, "OutbackAS", "https://localhost");
            var identityToken = await JwtValidationHelper.ValidateToken(client, tokenResponse.IdentityToken, clientId, "https://localhost");

            Assert.Equal(8, accessToken.Claims.Count());
            Assert.Equal("PKCEWebClientId", accessToken.Claims.Single(m => m.Key == "client_id").Value);
            Assert.Equal("openid", accessToken.Claims.Single(m => m.Key == "scope").Value);
            Assert.Equal("Test user", accessToken.Claims.Single(m => m.Key == "sub").Value);
            Assert.Equal("https://localhost", accessToken.Claims.Single(m => m.Key == "iss").Value);
            var audienceClaim = accessToken.Claims.Single(m => m.Key == "aud");
            Assert.Contains((List<object>)audienceClaim.Value, m => m as string == "OutbackAS");
            Assert.Contains((List<object>)audienceClaim.Value, m => m as string == "MessagingServer");
            Assert.Single(accessToken.Claims, m => m.Key == "exp");
            Assert.Single(accessToken.Claims, m => m.Key == "nbf");
            Assert.Single(accessToken.Claims, m => m.Key == "iat");

            Assert.Equal(6, identityToken.Claims.Count());
            Assert.Equal("Test user", identityToken.Claims.Single(m => m.Key == "sub").Value);
            Assert.Equal("https://localhost", identityToken.Claims.Single(m => m.Key == "iss").Value);
            Assert.Equal("PKCEWebClientId", identityToken.Claims.Single(m => m.Key == "aud").Value);
            Assert.Single(identityToken.Claims, m => m.Key == "exp");
            Assert.Single(identityToken.Claims, m => m.Key == "nbf");
            Assert.Single(identityToken.Claims, m => m.Key == "iat");
        }

        private static async Task<AccessTokenResponse?> GetTokenResponse(HttpClient client, string codeVerifier, string code)
        {
            var encodedClientIdAndSecret = Base64UrlEncoder.Encode("PKCEWebClientId:pwd");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedClientIdAndSecret);

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("code_verifier", codeVerifier),
            });

            var tokenResponse = await client.PostAsync("connect/token", formContent);
            tokenResponse.EnsureSuccessStatusCode();

            return await tokenResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();
        }

        private async Task<string> GetCodeAsync(string codeVerifier, HttpClient client, string clientId)
        {
            using var sha256 = SHA256.Create();
            var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            var codeChallenge = WebEncoders.Base64UrlEncode(challengeBytes);
            var uri = "connect/authorize";
            uri = QueryHelpers.AddQueryString(uri, "response_type", "code");
            uri = QueryHelpers.AddQueryString(uri, "client_id", clientId);
            uri = QueryHelpers.AddQueryString(uri, "code_challenge", codeChallenge);
            uri = QueryHelpers.AddQueryString(uri, "scope", "openid");

            // Act
            var authorizeGetResponse = await client.GetAsync(uri);
            var authorizeGetResponseContent = await authorizeGetResponse.Content.ReadAsStringAsync();
            var locationUri = authorizeGetResponse.Headers.Location;
            var queryParameters = QueryHelpers.ParseQuery(locationUri?.Query);

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

            return queryParameters["code"][0]; ;
        }
    }

    public class AccessTokenResponse
    {
        [JsonPropertyName("id_token")]
        public string IdentityToken { get; set; } = string.Empty;

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
