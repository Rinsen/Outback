using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Rinsen.IdentityProvider.Outback;
using Rinsen.IdentityProvider.Outback.Entities;
using Rinsen.Outback.Grants;
using Xunit;

namespace Rinsen.IdentityProvider.Tests;

/// <summary>
/// SEC-005: Device-code single-use semantics in the real GrantAccessor (RFC 8628 §3.2).
/// </summary>
public class DeviceCodeGrantTests
{
    private const string ClientId = "TestClient";
    private const string DeviceCode = "device-code-1";

    [Fact]
    public async Task GetDeviceAuthorizationGrantAsync_UnresolvedGrant_ReturnsGrantWithResolvedNull()
    {
        // Arrange
        var options = CreateOptions();
        await using (var context = new OutbackDbContext(options))
        {
            await SeedGrantAsync(context, resolved: null);
        }

        var accessor = new GrantAccessor(new OutbackDbContext(options));

        // Act
        var grant = await accessor.GetDeviceAuthorizationGrantAsync(DeviceCode);

        // Assert
        Assert.Equal(ClientId, grant.ClientId);
        Assert.Equal(DeviceCode, grant.DeviceCode);
        Assert.Null(grant.Resolved);
    }

    [Fact]
    public async Task GetDeviceAuthorizationGrantAsync_ResolvedGrant_ReportsItAsResolved()
    {
        // Arrange
        var options = CreateOptions();
        await using (var context = new OutbackDbContext(options))
        {
            await SeedGrantAsync(context, resolved: DateTimeOffset.UtcNow);
        }

        var accessor = new GrantAccessor(new OutbackDbContext(options));

        // Act
        var grant = await accessor.GetDeviceAuthorizationGrantAsync(DeviceCode);

        // Assert: the grant is still found and reported as already resolved
        Assert.NotNull(grant.Resolved);
    }

    [Fact]
    public async Task ResolveDeviceAuthorizationGrantAsync_MarksGrantResolved()
    {
        // Arrange
        var options = CreateOptions();
        await using (var context = new OutbackDbContext(options))
        {
            await SeedGrantAsync(context, resolved: null);
        }

        var accessor = new GrantAccessor(new OutbackDbContext(options));

        // Act
        await accessor.ResolveDeviceAuthorizationGrantAsync(DeviceCode);

        // Assert
        await using (var context = new OutbackDbContext(options))
        {
            var stored = await context.DeviceAuthorizationGrants.SingleAsync(m => m.DeviceCode == DeviceCode);
            Assert.NotNull(stored.Resolved);
        }
    }

    [Fact]
    public async Task SaveDeviceAuthorizationGrantAsync_PersistsGrant()
    {
        // Arrange
        var options = CreateOptions();
        await using (var context = new OutbackDbContext(options))
        {
            context.Clients.Add(new OutbackClient { ClientId = ClientId });
            await context.SaveChangesAsync();
        }

        var accessor = new GrantAccessor(new OutbackDbContext(options));

        // Act
        await accessor.SaveDeviceAuthorizationGrantAsync(new DeviceCodeGrant
        {
            ClientId = ClientId,
            SubjectId = null,
            DeviceCode = DeviceCode,
            UserCode = "ABCD-EFGH",
            Scope = "openid",
            UserCodeExpiration = DateTimeOffset.UtcNow.AddHours(1),
            Interval = 5,
        });

        // Assert
        await using (var context = new OutbackDbContext(options))
        {
            var stored = await context.DeviceAuthorizationGrants.SingleAsync(m => m.DeviceCode == DeviceCode);
            Assert.Equal("ABCD-EFGH", stored.UserCode);
            Assert.Equal("openid", stored.Scope);
            Assert.Null(stored.Resolved);
        }
    }

    private static async Task SeedGrantAsync(OutbackDbContext context, DateTimeOffset? resolved)
    {
        var client = new OutbackClient { ClientId = ClientId };
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        context.DeviceAuthorizationGrants.Add(new OutbackDeviceAuthorizationGrant
        {
            ClientId = client.Id,
            SubjectId = Guid.NewGuid(),
            DeviceCode = DeviceCode,
            UserCode = "ABCD-EFGH",
            Scope = "openid",
            Created = DateTimeOffset.UtcNow,
            UserCodeExpiration = DateTimeOffset.UtcNow.AddHours(1),
            Interval = 5,
            Resolved = resolved,
        });

        await context.SaveChangesAsync();
    }

    private static DbContextOptions<OutbackDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<OutbackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }
}
