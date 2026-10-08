using EventTicketing.Api.Data;
using EventTicketing.Api.Services;
using EventTicketing.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventTicketing.Tests.Services;

[Trait("Category", "Integration")]
public class RefreshTokenServiceTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private AppDbContext CreateDb() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    private RefreshTokenService CreateService(AppDbContext db) =>
        new(db, Options.Create(new JwtOptions { RefreshTokenDays = 7 }));

    private async Task<int> JohnIdAsync(AppDbContext db) =>
        (await db.Users.SingleAsync(u => u.Email == "john@example.com", TestContext.Current.CancellationToken)).Id;

    [Fact]
    public async Task IssueAsync_StoresOnlyTheHashOfTheReturnedToken()
    {
        var db = CreateDb();
        var familyId = Guid.NewGuid();

        var rawToken = await CreateService(db).IssueAsync(await JohnIdAsync(db), familyId, TestContext.Current.CancellationToken);

        var stored = await CreateDb().RefreshTokens.SingleAsync(t => t.FamilyId == familyId, TestContext.Current.CancellationToken);
        Assert.Equal(RefreshTokenService.Hash(rawToken), stored.TokenHash);
        Assert.NotEqual(rawToken, stored.TokenHash);
    }

    [Fact]
    public async Task IssueAsync_ExpiresAfterConfiguredDaysAndIsNotRevoked()
    {
        var db = CreateDb();
        var familyId = Guid.NewGuid();

        await CreateService(db).IssueAsync(await JohnIdAsync(db), familyId, TestContext.Current.CancellationToken);

        var stored = await CreateDb().RefreshTokens.SingleAsync(t => t.FamilyId == familyId, TestContext.Current.CancellationToken);
        Assert.InRange(stored.ExpiresAt, DateTime.UtcNow.AddDays(7).AddMinutes(-1), DateTime.UtcNow.AddDays(7).AddMinutes(1));
        Assert.Null(stored.RevokedAt);
    }

    [Fact]
    public async Task IssueAsync_ReturnsADifferentUrlSafeTokenEachTime()
    {
        var db = CreateDb();
        var service = CreateService(db);
        var userId = await JohnIdAsync(db);

        var first = await service.IssueAsync(userId, Guid.NewGuid(), TestContext.Current.CancellationToken);
        var second = await service.IssueAsync(userId, Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.NotEqual(first, second);
        Assert.DoesNotContain('+', first + second);
        Assert.DoesNotContain('/', first + second);
    }
}
