using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Services;

public class EventsService(AppDbContext db)
{
    /// <returns>The page, or null when the cursor is malformed.</returns>
    public async Task<EventPage?> ListAsync(EventListQuery query, CancellationToken cancellationToken)
    {
        var events = db.Events.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = "%" + EscapeLike(query.Q.Trim()) + "%";
            events = events.Where(e => EF.Functions.ILike(e.Title, pattern));
        }

        if (query.After is not null)
        {
            if (!EventCursor.TryDecode(query.After, query.Sort, out var key, out var id))
                return null;
            var afterCursor = AfterCursor(events, query.Sort, key, id);
            if (afterCursor is null)
                return null;
            events = afterCursor;
        }

        // One extra row tells us whether another page exists.
        var rows = await Ordered(events, query.Sort)
            .Take(query.Limit + 1)
            .Select(e => new EventResponse(
                e.Id, e.Title, e.Description, e.StartTime, e.EndTime, e.Venue, e.EventType,
                e.TotalSeats, e.AvailableSeats, e.PriceCents, e.ImageUrl))
            .ToListAsync(cancellationToken);

        var items = rows.Take(query.Limit).ToList();
        var nextCursor = rows.Count > query.Limit
            ? EventCursor.Encode(query.Sort, SortKey(items[^1], query.Sort), items[^1].Id)
            : null;
        return new EventPage(items, nextCursor);
    }

    private static IQueryable<Event> Ordered(IQueryable<Event> events, string sort) => sort switch
    {
        _ => events.OrderBy(e => e.Title).ThenBy(e => e.Id),
    };

    private static string SortKey(EventResponse e, string sort) => sort switch
    {
        _ => e.Title,
    };

    // Keyset predicate: rows strictly after (key, id) in the active sort order; null if the key doesn't parse.
    private static IQueryable<Event>? AfterCursor(IQueryable<Event> events, string sort, string key, int id)
    {
        switch (sort)
        {
            default:
                return events.Where(e => string.Compare(e.Title, key) > 0 || (e.Title == key && e.Id > id));
        }
    }

    // Treat the search text literally: LIKE wildcards in user input must not act as wildcards.
    private static string EscapeLike(string text) =>
        text.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
