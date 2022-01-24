using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
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

            var codeVerifier = "abcdefghijklmnopqrstuvqyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~0123456789";
            var code = await GetCodeAsync(codeVerifier, client);

            var tokenResponse = await GetTokenResponse(client, codeVerifier, code);

            if (tokenResponse == null)
                throw new Exception("Token response is null");
            
            var accessToken = await ValidateToken(client, tokenResponse.AccessToken);
            var identityToken = await ValidateToken(client, tokenResponse.IdentityToken);

            Assert.Equal(8, accessToken.Claims.Count());
            Assert.Equal("PKCEWebClientId", accessToken.Claims.Single(m => m.Type == "client_id").Value);
            Assert.Equal("openid", accessToken.Claims.Single(m => m.Type == "scope").Value);
            Assert.Equal("Test user", accessToken.Claims.Single(m => m.Type == "sub").Value);
            Assert.Equal("https://localhost", accessToken.Claims.Single(m => m.Type == "iss").Value);
            Assert.Equal("PKCEWebClientId", accessToken.Claims.Single(m => m.Type == "aud").Value);
            Assert.Single(accessToken.Claims, m => m.Type == "exp");
            Assert.Single(accessToken.Claims, m => m.Type == "nbf");
            Assert.Single(accessToken.Claims, m => m.Type == "iat");

            Assert.Equal(6, identityToken.Claims.Count());
            Assert.Equal("Test user", identityToken.Claims.Single(m => m.Type == "sub").Value);
            Assert.Equal("https://localhost", identityToken.Claims.Single(m => m.Type == "iss").Value);
            Assert.Equal("PKCEWebClientId", identityToken.Claims.Single(m => m.Type == "aud").Value);
            Assert.Single(identityToken.Claims, m => m.Type == "exp");
            Assert.Single(identityToken.Claims, m => m.Type == "nbf");
            Assert.Single(identityToken.Claims, m => m.Type == "iat");
        }

        private static async Task<JwtSecurityToken> ValidateToken(HttpClient client, string token)
        {
            var key = await client.GetJsonWebKey();
            var securityKey = new JsonWebKey()
            {
                KeyId = key.KeyId,
                Kty = key.KeyType,
                Alg = key.SigningAlgorithm,
                X = key.X,
                Y = key.Y,
                Crv = key.Curve,
            };

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidAudience = "PKCEWebClientId",
                ValidIssuer = "https://localhost",
                ValidateIssuerSigningKey = true,

                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidateAudience = true,
                ClockSkew = TimeSpan.Zero
            }, out var securityToken);

            return (JwtSecurityToken)securityToken;
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

        private async Task<string> GetCodeAsync(string codeVerifier, HttpClient client)
        {
            using var sha256 = SHA256.Create();
            var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            var codeChallenge = WebEncoders.Base64UrlEncode(challengeBytes);
            var uri = "connect/authorize";
            uri = QueryHelpers.AddQueryString(uri, "response_type", "code");
            uri = QueryHelpers.AddQueryString(uri, "client_id", "PKCEWebClientId");
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
