using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Api.Services;
using EventTicketing.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Services;

[Trait("Category", "Integration")]
public class DemoDateRefresherTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private AppDbContext CreateDb() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    private Task<int> RefreshAsync() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<DemoDateRefresher>()
            .RefreshAsync(Today, TestContext.Current.CancellationToken);

    // As if the database had been left alone for 40 days.
    private async Task AgeTheDemoAsync()
    {
        var titles = EventSeeder.DemoSchedule(Today).Keys.ToList();
        await CreateDb().Events.Where(e => titles.Contains(e.Title)).ExecuteUpdateAsync(s => s
            .SetProperty(e => e.StartTime, e => e.StartTime.AddDays(-40))
            .SetProperty(e => e.EndTime, e => e.EndTime.AddDays(-40)), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task WhileTheDemoIsUpcoming_NothingMoves()
    {
        Assert.Equal(0, await RefreshAsync());
    }

    [Fact]
    public async Task OnceTheDemoHasStarted_ItMovesToStartTomorrow()
    {
        await AgeTheDemoAsync();

        var moved = await RefreshAsync();

        Assert.Equal(30, moved);
        var jazz = await CreateDb().Events.SingleAsync(e => e.Title == "Jazz Night", TestContext.Current.CancellationToken);
        var local = TimeZoneInfo.ConvertTimeFromUtc(jazz.StartTime, NewYork);
        Assert.Equal(Today.AddDays(1), DateOnly.FromDateTime(local));
        Assert.Equal(new TimeOnly(19, 0), TimeOnly.FromDateTime(local));
        Assert.True(await CreateDb().Events.MinAsync(e => e.StartTime, TestContext.Current.CancellationToken) > DateTime.UtcNow);
    }

    [Fact]
    public async Task EventsAnAdminCreated_StayWhereTheyAre()
    {
        var db = CreateDb();
        var own = new Event
        {
            Title = $"Admin's own {Guid.NewGuid():N}"[..20],
            Description = "Not a demo event.",
            StartTime = DateTime.UtcNow.AddDays(-3),
            EndTime = DateTime.UtcNow.AddDays(-3).AddHours(2),
            Venue = "Back Room",
            TotalSeats = 10,
            AvailableSeats = 10,
        };
        db.Events.Add(own);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await AgeTheDemoAsync();

        await RefreshAsync();

        var reloaded = await CreateDb().Events.SingleAsync(e => e.Id == own.Id, TestContext.Current.CancellationToken);
        Assert.Equal(own.StartTime, reloaded.StartTime, TimeSpan.FromMilliseconds(1));
    }
}
