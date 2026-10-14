using EventTicketing.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace EventTicketing.Api.Services;

/// <summary>
/// Keeps a long-lived database's demo events upcoming: once the earliest of them has started, all of
/// them move so the demo begins tomorrow again, with the seeder's spacing and times of day. Events an
/// admin created are never touched; orders stay with their (moved) events.
/// </summary>
public class DemoDateRefresher(AppDbContext db, HybridCache cache, ILogger<DemoDateRefresher> logger)
{
    /// <returns>How many events moved (0 while the demo is still upcoming).</returns>
    public async Task<int> RefreshAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var schedule = EventSeeder.DemoSchedule(today);
        var titles = schedule.Keys.ToList();
        var events = await db.Events.Where(e => titles.Contains(e.Title)).ToListAsync(cancellationToken);
        if (events.Count == 0 || events.Min(e => e.StartTime) > DateTime.UtcNow)
            return 0;

        foreach (var ev in events)
            (ev.StartTime, ev.EndTime) = schedule[ev.Title];
        await db.SaveChangesAsync(cancellationToken);
        // Cached list pages still hold the old dates.
        await cache.RemoveByTagAsync(EventsService.ListCacheTag, CancellationToken.None);

        logger.LogInformation("Moved {Count} demo events to start on {FirstDay}", events.Count, today.AddDays(1));
        return events.Count;
    }
}
