using EventTicketing.Api.Data;
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
}
