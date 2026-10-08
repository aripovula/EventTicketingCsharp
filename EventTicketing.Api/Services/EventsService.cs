using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Services;

public class EventsService(AppDbContext db)
{
    public async Task<EventPage> ListAsync(EventListQuery query, CancellationToken cancellationToken)
    {
        var events = db.Events.AsQueryable();
        if (query.After is not null)
        {
            var (key, id) = EventCursor.Decode(query.After);
            events = events.Where(e => string.Compare(e.Title, key) > 0 || (e.Title == key && e.Id > id));
        }

        // One extra row tells us whether another page exists.
        var rows = await events
            .OrderBy(e => e.Title).ThenBy(e => e.Id)
            .Take(query.Limit + 1)
            .Select(e => new EventResponse(
                e.Id, e.Title, e.Description, e.StartTime, e.EndTime, e.Venue, e.EventType,
                e.TotalSeats, e.AvailableSeats, e.PriceCents, e.ImageUrl))
            .ToListAsync(cancellationToken);

        var items = rows.Take(query.Limit).ToList();
        var nextCursor = rows.Count > query.Limit ? EventCursor.Encode(items[^1].Title, items[^1].Id) : null;
        return new EventPage(items, nextCursor);
    }
}
