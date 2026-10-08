using EventTicketing.Api.Data;
using EventTicketing.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Data;

[Trait("Category", "Integration")]
public class EventSeederTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private AppDbContext CreateDb() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task SeedsTheThirtyDemoEventsOnStartup()
    {
        Assert.Equal(30, await CreateDb().Events.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AllSeededEventsAreUpcoming()
    {
        var earliest = await CreateDb().Events.MinAsync(e => e.StartTime, TestContext.Current.CancellationToken);

        Assert.True(earliest > DateTime.UtcNow);
    }

    [Fact]
    public async Task JazzNightStartsAt19NewYorkTimeOnTheFirstDemoDay()
    {
        var jazz = await CreateDb().Events.SingleAsync(e => e.Title == "Jazz Night", TestContext.Current.CancellationToken);

        var local = TimeZoneInfo.ConvertTimeFromUtc(jazz.StartTime, TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));
        Assert.Equal(new TimeOnly(19, 0), TimeOnly.FromDateTime(local));
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), DateOnly.FromDateTime(local));
        Assert.Equal(2500, jazz.PriceCents);
    }

    [Fact]
    public async Task SeedingAgainDoesNotDuplicateEvents()
    {
        var db = CreateDb();

        EventSeeder.Seed(db, DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.Equal(30, await db.Events.CountAsync(TestContext.Current.CancellationToken));
    }
}
