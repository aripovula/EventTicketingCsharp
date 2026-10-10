using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Api.Services;
using EventTicketing.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Services;

[Trait("Category", "Integration")]
public class IdempotencyServiceTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private AppDbContext CreateDb() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task Store_KeepsTheResponseFor24Hours()
    {
        var key = Guid.NewGuid().ToString();

        await new IdempotencyService(CreateDb()).StoreAsync(key, "/api/v1/events/1/bookings", 201, """{"data":{"id":7},"errors":[]}""", TestContext.Current.CancellationToken);

        var saved = await CreateDb().IdempotencyKeys.SingleAsync(k => k.Key == key, TestContext.Current.CancellationToken);
        Assert.Equal("/api/v1/events/1/bookings", saved.RequestPath);
        Assert.Equal(201, saved.StatusCode);
        Assert.Contains("\"id\": 7", saved.ResponseBody);
        Assert.Equal(TimeSpan.FromHours(24), saved.ExpiresAt - saved.CreatedAt);
    }

    [Fact]
    public async Task Store_RejectsTheSameKeyForTheSamePathTwice()
    {
        var key = Guid.NewGuid().ToString();
        await new IdempotencyService(CreateDb()).StoreAsync(key, "/a", 201, "{}", TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            new IdempotencyService(CreateDb()).StoreAsync(key, "/a", 201, "{}", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Get_ReturnsTheStoredResponseForTheSameKeyAndPath()
    {
        var key = Guid.NewGuid().ToString();
        await new IdempotencyService(CreateDb()).StoreAsync(key, "/a", 201, "{}", TestContext.Current.CancellationToken);

        var found = await new IdempotencyService(CreateDb()).GetAsync(key, "/a", TestContext.Current.CancellationToken);

        Assert.Equal(201, found?.StatusCode);
    }

    [Fact]
    public async Task Get_IgnoresTheSameKeyOnAnotherPath()
    {
        var key = Guid.NewGuid().ToString();
        await new IdempotencyService(CreateDb()).StoreAsync(key, "/a", 201, "{}", TestContext.Current.CancellationToken);

        Assert.Null(await new IdempotencyService(CreateDb()).GetAsync(key, "/b", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Get_IgnoresAnExpiredResponse()
    {
        var key = Guid.NewGuid().ToString();
        var db = CreateDb();
        db.IdempotencyKeys.Add(new IdempotencyKey
        {
            Key = key,
            RequestPath = "/a",
            StatusCode = 201,
            ResponseBody = "{}",
            CreatedAt = DateTime.UtcNow.AddHours(-25),
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Null(await new IdempotencyService(CreateDb()).GetAsync(key, "/a", TestContext.Current.CancellationToken));
    }
}
