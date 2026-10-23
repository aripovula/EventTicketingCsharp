using System.Globalization;
using System.Linq.Expressions;
using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace EventTicketing.Api.Services;

public class EventsService(AppDbContext db, HybridCache cache, AuditLogger audit)
{
    /// <summary>Tag on every cached event-list page, so one call evicts them all.</summary>
    public const string ListCacheTag = "events";

    // An expression (not a method) so EF Core translates it into the SELECT list.
    private static readonly Expression<Func<Event, EventResponse>> ToResponse = e => new EventResponse(
        e.Id, e.Title, e.Description, e.StartTime, e.EndTime, e.Venue, e.EventType,
        e.TotalSeats, e.AvailableSeats, e.PriceCents, e.ImageUrl, e.Orders.Any(), e.ArchivedAt);

    /// <returns>The page, or null when the cursor is malformed.</returns>
    public async Task<EventPage?> ListAsync(EventListQuery query, CancellationToken cancellationToken)
    {
        // Everyone loads the plain list pages; searches are too varied to be worth caching.
        if (!string.IsNullOrWhiteSpace(query.Q))
            return await QueryPageAsync(query, cancellationToken);

        return await cache.GetOrCreateAsync(
            $"events:sort:{query.Sort}:after:{query.After}:limit:{query.Limit}",
            query,
            (q, token) => new ValueTask<EventPage?>(QueryPageAsync(q, token)),
            tags: [ListCacheTag],
            cancellationToken: cancellationToken);
    }

    private async Task<EventPage?> QueryPageAsync(EventListQuery query, CancellationToken cancellationToken)
    {
        var events = db.Events.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = "%" + EscapeLike(query.Q.Trim()) + "%";
            events = events.Where(e => EF.Functions.ILike(e.Title, pattern));
        }

        if (query.After is not null)
        {
            if (!PageCursor.TryDecode(query.After, query.Sort, out var key, out var id))
                return null;
            var afterCursor = AfterCursor(events, query.Sort, key, id);
            if (afterCursor is null)
                return null;
            events = afterCursor;
        }

        // One extra row tells us whether another page exists.
        var rows = await Ordered(events, query.Sort)
            .Take(query.Limit + 1)
            .Select(ToResponse)
            .ToListAsync(cancellationToken);

        var items = rows.Take(query.Limit).ToList();
        var nextCursor = rows.Count > query.Limit
            ? PageCursor.Encode(query.Sort, SortKey(items[^1], query.Sort), items[^1].Id)
            : null;
        return new EventPage(items, nextCursor);
    }

    public Task<EventResponse?> GetAsync(int id, CancellationToken cancellationToken) =>
        db.Events.Where(e => e.Id == id).Select(ToResponse).SingleOrDefaultAsync(cancellationToken);

    public async Task<EventResponse> CreateAsync(EventRequest request, AuditActor actor, CancellationToken cancellationToken)
    {
        var ev = new Event
        {
            Title = request.Title,
            Description = request.Description,
            StartTime = request.StartTime.ToUniversalTime(),
            EndTime = request.EndTime.ToUniversalTime(),
            Venue = request.Venue,
            EventType = request.EventType,
            TotalSeats = request.TotalSeats,
            AvailableSeats = request.AvailableSeats,
            PriceCents = request.PriceCents,
            ImageUrl = request.ImageUrl,
        };
        // The audit entry needs the new id, so save twice inside one transaction.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Events.Add(ev);
        await db.SaveChangesAsync(cancellationToken);
        audit.Record(AuditAction.EventCreated, "Event", ev.Id, actor, new { ev.Title });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await EvictListPagesAsync();

        return ToResponseCompiled(ev);
    }

    /// <returns>False when no event has the given id.</returns>
    public async Task<bool> UpdateAsync(int id, EventRequest request, AuditActor actor, CancellationToken cancellationToken)
    {
        var ev = await db.Events.FindAsync([id], cancellationToken);
        if (ev is null)
            return false;

        ev.Title = request.Title;
        ev.Description = request.Description;
        ev.StartTime = request.StartTime.ToUniversalTime();
        ev.EndTime = request.EndTime.ToUniversalTime();
        ev.Venue = request.Venue;
        ev.EventType = request.EventType;
        ev.TotalSeats = request.TotalSeats;
        ev.AvailableSeats = request.AvailableSeats;
        ev.PriceCents = request.PriceCents;
        ev.ImageUrl = request.ImageUrl;
        audit.Record(AuditAction.EventUpdated, "Event", id, actor, new { ev.Title });
        await db.SaveChangesAsync(cancellationToken);
        await EvictListPagesAsync();
        return true;
    }

    public async Task<DeleteResult> DeleteAsync(int id, AuditActor actor, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Read the title for the audit entry before the row is gone.
        var title = await db.Events.Where(e => e.Id == id).Select(e => e.Title).SingleOrDefaultAsync(cancellationToken);
        if (title is null)
            return DeleteResult.NotFound;
        // Deleting would take the orders with it (they cascade), so an event with orders is archived instead.
        if (await db.Events.Where(e => e.Id == id && !e.Orders.Any()).ExecuteDeleteAsync(cancellationToken) == 0)
            return DeleteResult.HasOrders;

        audit.Record(AuditAction.EventDeleted, "Event", id, actor, new { Title = title });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await EvictListPagesAsync();
        return DeleteResult.Deleted;
    }

    // The change is already saved, so evict even if the request is cancelled meanwhile.
    private ValueTask EvictListPagesAsync() => cache.RemoveByTagAsync(ListCacheTag, CancellationToken.None);

    public async Task<IReadOnlyList<EventSummary>> SummaryAsync(CancellationToken cancellationToken) =>
        await db.Events
            .OrderBy(e => e.StartTime).ThenBy(e => e.Id)
            .Select(e => new EventSummary(
                e.Id,
                e.Title,
                e.TotalSeats,
                e.TotalSeats - e.AvailableSeats,
                e.AvailableSeats,
                (e.TotalSeats - e.AvailableSeats) * e.PriceCents))
            .ToListAsync(cancellationToken);

    private static readonly Func<Event, EventResponse> ToResponseCompiled = ToResponse.Compile();

    private static IQueryable<Event> Ordered(IQueryable<Event> events, string sort) => sort switch
    {
        "date" => events.OrderBy(e => e.StartTime).ThenBy(e => e.Id),
        "price" => events.OrderBy(e => e.PriceCents).ThenBy(e => e.Id),
        _ => events.OrderBy(e => e.Title).ThenBy(e => e.Id),
    };

    private static string SortKey(EventResponse e, string sort) => sort switch
    {
        "date" => e.StartTime.ToString("O", CultureInfo.InvariantCulture),
        "price" => e.PriceCents.ToString(CultureInfo.InvariantCulture),
        _ => e.Title,
    };

    // Keyset predicate: rows strictly after (key, id) in the active sort order; null if the key doesn't parse.
    private static IQueryable<Event>? AfterCursor(IQueryable<Event> events, string sort, string key, int id)
    {
        switch (sort)
        {
            case "date":
                if (!DateTime.TryParse(key, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var start))
                    return null;
                start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
                return events.Where(e => e.StartTime > start || (e.StartTime == start && e.Id > id));
            case "price":
                if (!int.TryParse(key, CultureInfo.InvariantCulture, out var price))
                    return null;
                return events.Where(e => e.PriceCents > price || (e.PriceCents == price && e.Id > id));
            default:
                return events.Where(e => string.Compare(e.Title, key) > 0 || (e.Title == key && e.Id > id));
        }
    }

    // Treat the search text literally: LIKE wildcards in user input must not act as wildcards.
    private static string EscapeLike(string text) =>
        text.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}

public enum DeleteResult
{
    Deleted,
    NotFound,
    HasOrders,
}
