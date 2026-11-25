using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rinsen.IdentityProvider;
using Rinsen.IdentityProvider.Outback;
using Rinsen.IdentityProvider.Outback.Entities;
using Rinsen.Outback.App.Installation;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// SEC-019: the installer must never log the client secret. The seed step
    /// logs the ClientId so the developer can record it, but the secret value
    /// must not appear anywhere in the install output.
    /// </summary>
    public class InstallationSecretLoggingTests
    {
        private const string ClientId = "sec019-test-client-id";
        private const string ClientSecret = "sec019-super-secret-value-424242";

        [Fact]
        public async Task SeedData_LogsClientId_ButNeverTheSecret()
        {
            // Arrange: a known client id/secret via configuration (DefaultInstaller
            // prefers Rinsen:ClientId / Rinsen:ClientSecret over generated values),
            // an in-memory database, and a capturing logger.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Rinsen:ClientId", ClientId },
                    { "Rinsen:ClientSecret", ClientSecret }
                })
                .Build();

            var options = new DbContextOptionsBuilder<OutbackDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var logger = new CapturingLogger<OutbackDataSeed>();

            await using var context = new OutbackDbContext(options);

            var seed = new OutbackDataSeed(
                new DefaultInstaller(context, new RandomStringGenerator(), configuration),
                logger);

            // Act
            await seed.SeedData();

            // Assert: the ClientId is logged (so the developer can record it),
            // and no log message contains the secret value.
            Assert.True(logger.Messages.Any(m => m.Contains(ClientId)),
                "the ClientId should be logged so the developer can record it");
            Assert.True(logger.Messages.All(m => !m.Contains(ClientSecret)),
                "no log message may contain the client secret");
        }

        private sealed class CapturingLogger<T> : ILogger<T>
        {
            public List<string> Messages { get; } = new();

            public IDisposable BeginScope<TState>(TState state) where TState : notnull
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                lock (Messages)
                {
                    Messages.Add(formatter(state, exception));
                }
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();

                public void Dispose()
                {
                }
            }
        }
    }
}
