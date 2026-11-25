using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using SampleServer;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// SEC-016: the credential endpoint (/oauth/token) is rate limited. When the
    /// configured per-window permit limit is exceeded, further requests receive
    /// HTTP 429 (Too Many Requests).
    /// </summary>
    public class RateLimitingTests
    {
        [Fact]
        public async Task TokenEndpoint_ExceedingPermitLimit_Returns429()
        {
            // Arrange: set a low permit limit (2 per window) so the 3rd request is rejected.
            var application = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string>
                        {
                            { "Rinsen:RateLimiting:PermitLimit", "2" },
                            { "Rinsen:RateLimiting:WindowSeconds", "60" }
                        });
                    });
                });

            var client = application.CreateClient();

            // Act: the first two requests are within the limit, the third exceeds it.
            var first = await client.PostAsync("oauth/token", CreateFormContent());
            var second = await client.PostAsync("oauth/token", CreateFormContent());
            var third = await client.PostAsync("oauth/token", CreateFormContent());

            // Assert
            Assert.True(first.StatusCode != HttpStatusCode.TooManyRequests,
                $"first request should be allowed, got {(int)first.StatusCode}");
            Assert.True(second.StatusCode != HttpStatusCode.TooManyRequests,
                $"second request should be allowed, got {(int)second.StatusCode}");
            Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        }

        private static FormUrlEncodedContent CreateFormContent()
        {
            return new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", "MachineToMachineClientId"),
                new KeyValuePair<string, string>("client_secret", "pwd")
            });
        }
    }
}
