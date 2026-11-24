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
/// SEC-004: Refresh-token family revocation in the real GrantAccessor.
/// </summary>
public class RefreshTokenFamilyTests
{
    private const string ClientId = "TestClient";

    [Fact]
    public async Task GetRefreshTokenGrantAsync_UnresolvedGrant_ResolvesIt_AndReportsItAsUnresolved()
    {
        // Arrange
        var options = CreateOptions();
        await using (var context = new OutbackDbContext(options))
        {
            await SeedGrantAsync(context, "token-1", Guid.NewGuid(), resolved: null);
        }

        var accessor = new GrantAccessor(new OutbackDbContext(options));

        // Act
        var grant = await accessor.GetRefreshTokenGrantAsync("token-1");

        // Assert: reported as unresolved (first use) ...
        Assert.Null(grant.Resolved);

        // ... but resolved in the database
        await using (var context = new OutbackDbContext(options))
        {
            var stored = await context.RefreshTokenGrants.SingleAsync(m => m.RefreshToken == "token-1");
            Assert.NotNull(stored.Resolved);
        }
    }

    [Fact]
    public async Task GetRefreshTokenGrantAsync_ResolvedGrant_ReportsItAsResolved()
    {
        // Arrange
        var options = CreateOptions();
        await using (var context = new OutbackDbContext(options))
        {
            await SeedGrantAsync(context, "token-1", Guid.NewGuid(), resolved: DateTimeOffset.UtcNow);
        }

        var accessor = new GrantAccessor(new OutbackDbContext(options));

        // Act
        var grant = await accessor.GetRefreshTokenGrantAsync("token-1");

        // Assert: the grant is still found and reported as already resolved
        Assert.NotNull(grant.Resolved);
    }

    [Fact]
    public async Task RevokeRefreshTokenFamilyAsync_MarksAllGrantsInFamilyResolved()
    {
        // Arrange: a rotation chain (two grants in one family) plus a grant in another family
        var familyId = Guid.NewGuid();
        var options = CreateOptions();
        await using (var context = new OutbackDbContext(options))
        {
            await SeedGrantAsync(context, "token-old", familyId, resolved: DateTimeOffset.UtcNow);
            await SeedGrantAsync(context, "token-live", familyId, resolved: null);
            await SeedGrantAsync(context, "token-other-family", Guid.NewGuid(), resolved: null);
        }

        var accessor = new GrantAccessor(new OutbackDbContext(options));

        // Act
        await accessor.RevokeRefreshTokenFamilyAsync(familyId);

        // Assert: every grant in the family is resolved, other families are untouched
        await using (var context = new OutbackDbContext(options))
        {
            Assert.NotNull((await context.RefreshTokenGrants.SingleAsync(m => m.RefreshToken == "token-old")).Resolved);
            Assert.NotNull((await context.RefreshTokenGrants.SingleAsync(m => m.RefreshToken == "token-live")).Resolved);
            Assert.Null((await context.RefreshTokenGrants.SingleAsync(m => m.RefreshToken == "token-other-family")).Resolved);
        }
    }

    [Fact]
    public async Task SaveRefreshTokenGrantAsync_PersistsFamilyId()
    {
        // Arrange
        var options = CreateOptions();
        await using (var context = new OutbackDbContext(options))
        {
            context.Clients.Add(new OutbackClient { ClientId = ClientId });
            await context.SaveChangesAsync();
        }

        var accessor = new GrantAccessor(new OutbackDbContext(options));
        var familyId = Guid.NewGuid();

        // Act
        await accessor.SaveRefreshTokenGrantAsync(new RefreshTokenGrant
        {
            ClientId = ClientId,
            SubjectId = Guid.NewGuid().ToString(),
            RefreshToken = "token-1",
            Scope = "openid",
            Created = DateTimeOffset.UtcNow,
            Expires = DateTimeOffset.UtcNow.AddHours(1),
            FamilyId = familyId,
        });

        // Assert
        await using (var context = new OutbackDbContext(options))
        {
            var stored = await context.RefreshTokenGrants.SingleAsync(m => m.RefreshToken == "token-1");
            Assert.Equal(familyId, stored.FamilyId);
        }
    }

    private static async Task SeedGrantAsync(OutbackDbContext context, string refreshToken, Guid familyId, DateTimeOffset? resolved)
    {
        var client = new OutbackClient { ClientId = ClientId };
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        context.RefreshTokenGrants.Add(new OutbackRefreshTokenGrant
        {
            ClientId = client.Id,
            SubjectId = Guid.NewGuid(),
            RefreshToken = refreshToken,
            Scope = "openid",
            Created = DateTimeOffset.UtcNow,
            Expires = DateTimeOffset.UtcNow.AddHours(1),
            Resolved = resolved,
            FamilyId = familyId,
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
