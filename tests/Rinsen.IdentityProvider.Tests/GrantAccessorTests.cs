using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Rinsen.IdentityProvider.Outback;
using Rinsen.IdentityProvider.Outback.Entities;
using Xunit;

namespace Rinsen.IdentityProvider.Tests;

/// <summary>
/// SEC-002: GrantAccessor exceptions must not embed the raw refresh token,
/// because they are logged by the grant handler on the failure path.
/// </summary>
public class GrantAccessorTests
{
    private const string RefreshToken = "super-secret-refresh-token-value";

    [Fact]
    public async Task GetRefreshTokenGrantAsync_TokenNotFound_ExceptionMessageContainsNoToken()
    {
        // Arrange
        var accessor = CreateAccessor();

        // Act
        var exception = await Assert.ThrowsAsync<Exception>(() => accessor.GetRefreshTokenGrantAsync(RefreshToken));

        // Assert
        Assert.False(exception.Message.Contains(RefreshToken));
    }

    [Fact]
    public async Task GetRefreshTokenGrantAsync_ClientNotFound_ExceptionMessageContainsNoToken()
    {
        // Arrange: a refresh-token grant whose client does not exist
        var options = CreateOptions();

        await using (var context = new OutbackDbContext(options))
        {
            context.RefreshTokenGrants.Add(new OutbackRefreshTokenGrant
            {
                ClientId = 999,
                SubjectId = Guid.NewGuid(),
                RefreshToken = RefreshToken,
                Scope = "openid",
                Created = DateTimeOffset.UtcNow,
                Expires = DateTimeOffset.UtcNow.AddHours(1),
            });

            await context.SaveChangesAsync();
        }

        var accessor = new GrantAccessor(new OutbackDbContext(options));

        // Act
        var exception = await Assert.ThrowsAsync<Exception>(() => accessor.GetRefreshTokenGrantAsync(RefreshToken));

        // Assert
        Assert.False(exception.Message.Contains(RefreshToken));
    }

    private static GrantAccessor CreateAccessor()
    {
        return new GrantAccessor(new OutbackDbContext(CreateOptions()));
    }

    private static DbContextOptions<OutbackDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<OutbackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }
}
