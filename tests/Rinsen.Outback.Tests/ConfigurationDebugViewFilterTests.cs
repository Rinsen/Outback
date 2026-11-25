using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rinsen.IdentityProvider.Backup;
using Rinsen.Outback.App.ApiControllers;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// SEC-023: the admin configuration dump must not contain connection strings,
    /// passwords, secrets, or invitation codes — neither the keys nor their values.
    /// Non-sensitive keys (ClientId, TotpIssuer) must still be visible.
    /// </summary>
    public class ConfigurationDebugViewFilterTests
    {
        private const string ConnectionString = "Server=outback-db;User Id=sa;Password=hunter2;Trust Server Certificate=True";
        private const string ClientSecret = "sec023-super-secret-value-424242";
        private const string InvitationCode = "sec023-invitation-code-424242";
        private const string SqlPassword = "sec023-sql-password-424242";
        private const string ClientId = "sec023-test-client-id";

        private static string GetConfigurationBody()
        {
            // Arrange: the keys the real app uses, with known sensitive values.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "ConnectionStrings:Outback", ConnectionString },
                    { "Rinsen:ClientSecret", ClientSecret },
                    { "Rinsen:InvitationCode", InvitationCode },
                    { "Rinsen:DbPassword", SqlPassword },
                    { "Rinsen:ClientId", ClientId },
                    { "Rinsen:TotpIssuer", "Outback" }
                })
                .Build();

            var controller = new AdminController(configuration, new BackupGenerator(null!));

            // Act
            var result = Assert.IsType<OkObjectResult>(controller.GetConfiguration());

            // The debug view is returned as a plain-text body.
            return Assert.IsType<string>(result.Value);
        }

        [Fact]
        public void GetConfiguration_ConnectionString_IsAbsent()
        {
            var body = GetConfigurationBody();

            Assert.DoesNotContain(ConnectionString, body);
            Assert.DoesNotContain("hunter2", body);
            Assert.DoesNotContain("outback-db", body);
            Assert.DoesNotContain("connectionstring", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetConfiguration_ClientSecret_IsAbsent()
        {
            var body = GetConfigurationBody();

            Assert.DoesNotContain(ClientSecret, body);
            Assert.DoesNotContain("clientsecret", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetConfiguration_InvitationCode_IsAbsent()
        {
            var body = GetConfigurationBody();

            Assert.DoesNotContain(InvitationCode, body);
            Assert.DoesNotContain("invitationcode", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetConfiguration_Password_IsAbsent()
        {
            var body = GetConfigurationBody();

            Assert.DoesNotContain(SqlPassword, body);
            Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetConfiguration_NonSensitiveKeys_ArePresent()
        {
            var body = GetConfigurationBody();

            Assert.Contains(ClientId, body);
            Assert.Contains("ClientId", body);
            Assert.Contains("TotpIssuer", body);
        }
    }
}
