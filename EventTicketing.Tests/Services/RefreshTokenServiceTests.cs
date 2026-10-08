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

    private async Task<string> IssueForJohnAsync(Action<Api.Models.RefreshToken>? tamper = null)
    {
        var db = CreateDb();
        var familyId = Guid.NewGuid();
        var rawToken = await CreateService(db).IssueAsync(await JohnIdAsync(db), familyId, TestContext.Current.CancellationToken);
        if (tamper is not null)
        {
            var stored = await db.RefreshTokens.SingleAsync(t => t.FamilyId == familyId, TestContext.Current.CancellationToken);
            tamper(stored);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        return rawToken;
    }

    [Fact]
    public async Task RotateAsync_ReturnsTheTokensUserForAValidToken()
    {
        var rawToken = await IssueForJohnAsync();

        var rotation = await CreateService(CreateDb()).RotateAsync(rawToken, TestContext.Current.CancellationToken);

        Assert.NotNull(rotation);
        Assert.Equal("john@example.com", rotation.User.Email);
    }

    [Fact]
    public async Task RotateAsync_RejectsAnUnknownToken()
    {
        var rotation = await CreateService(CreateDb()).RotateAsync("not-a-real-token", TestContext.Current.CancellationToken);

        Assert.Null(rotation);
    }

    [Fact]
    public async Task RotateAsync_RejectsAnExpiredToken()
    {
        var rawToken = await IssueForJohnAsync(t => t.ExpiresAt = DateTime.UtcNow.AddMinutes(-1));

        var rotation = await CreateService(CreateDb()).RotateAsync(rawToken, TestContext.Current.CancellationToken);

        Assert.Null(rotation);
    }

    [Fact]
    public async Task RotateAsync_RejectsARevokedToken()
    {
        var rawToken = await IssueForJohnAsync(t => t.RevokedAt = DateTime.UtcNow);

        var rotation = await CreateService(CreateDb()).RotateAsync(rawToken, TestContext.Current.CancellationToken);

        Assert.Null(rotation);
    }

    [Fact]
    public async Task RotateAsync_ReturnsANewTokenThatCanItselfBeRotated()
    {
        var rawToken = await IssueForJohnAsync();

        var rotation = await CreateService(CreateDb()).RotateAsync(rawToken, TestContext.Current.CancellationToken);
        var next = await CreateService(CreateDb()).RotateAsync(rotation!.Token, TestContext.Current.CancellationToken);

        Assert.NotEqual(rawToken, rotation.Token);
        Assert.NotNull(next);
    }

    [Fact]
    public async Task RotateAsync_RevokesTheOldTokenAndLinksItToItsReplacementInTheSameFamily()
    {
        var rawToken = await IssueForJohnAsync();

        var rotation = await CreateService(CreateDb()).RotateAsync(rawToken, TestContext.Current.CancellationToken);

        var db = CreateDb();
        var oldHash = RefreshTokenService.Hash(rawToken);
        var newHash = RefreshTokenService.Hash(rotation!.Token);
        var old = await db.RefreshTokens.SingleAsync(t => t.TokenHash == oldHash, TestContext.Current.CancellationToken);
        var replacement = await db.RefreshTokens.SingleAsync(t => t.TokenHash == newHash, TestContext.Current.CancellationToken);
        Assert.NotNull(old.RevokedAt);
        Assert.Equal(replacement.Id, old.ReplacedById);
        Assert.Equal(old.FamilyId, replacement.FamilyId);
        Assert.Null(replacement.RevokedAt);
    }

    [Fact]
    public async Task RotateAsync_ReusingARotatedTokenRevokesTheWholeFamily()
    {
        var rawToken = await IssueForJohnAsync();
        var rotation = await CreateService(CreateDb()).RotateAsync(rawToken, TestContext.Current.CancellationToken);

        var reuse = await CreateService(CreateDb()).RotateAsync(rawToken, TestContext.Current.CancellationToken);
        var afterReuse = await CreateService(CreateDb()).RotateAsync(rotation!.Token, TestContext.Current.CancellationToken);

        Assert.Null(reuse);
        Assert.Null(afterReuse);
    }

    [Fact]
    public async Task RotateAsync_ReuseLeavesOtherFamiliesUntouched()
    {
        var stolen = await IssueForJohnAsync();
        var otherSession = await IssueForJohnAsync();
        await CreateService(CreateDb()).RotateAsync(stolen, TestContext.Current.CancellationToken);

        await CreateService(CreateDb()).RotateAsync(stolen, TestContext.Current.CancellationToken);
        var other = await CreateService(CreateDb()).RotateAsync(otherSession, TestContext.Current.CancellationToken);

        Assert.NotNull(other);
    }
}
