using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Services;

public class EventsService(AppDbContext db)
{
    public async Task<EventPage> ListAsync(EventListQuery query, CancellationToken cancellationToken)
    {
        var items = await db.Events
            .OrderBy(e => e.Title).ThenBy(e => e.Id)
            .Take(query.Limit)
            .Select(e => new EventResponse(
                e.Id, e.Title, e.Description, e.StartTime, e.EndTime, e.Venue, e.EventType,
                e.TotalSeats, e.AvailableSeats, e.PriceCents, e.ImageUrl))
            .ToListAsync(cancellationToken);

        return new EventPage(items, null);
    }
}
