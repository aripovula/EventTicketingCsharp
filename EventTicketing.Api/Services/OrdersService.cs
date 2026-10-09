using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Services;

/// <summary>Who is asking for an order: a signed-in user (maybe admin) and/or a guest token holder.</summary>
public record OrderViewer(int? UserId, bool IsAdmin, string? GuestToken);

public class OrdersService(AppDbContext db)
{
    private const string CursorSort = "booked-desc";

    /// <returns>The order, or null when it doesn't exist or the viewer may not see it.</returns>
    public async Task<OrderResponse?> GetAsync(int orderId, OrderViewer viewer, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Where(o => o.Id == orderId)
            .Select(o => new { o.Id, o.EventId, EventTitle = o.Event.Title, o.Email, o.PriceCents, o.BookedAt, o.UserId, o.GuestAccessTokenHash })
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null)
            return null;

        var allowed = viewer.IsAdmin
            || (viewer.UserId is not null && viewer.UserId == order.UserId)
            || TokenMatches(viewer.GuestToken, order.GuestAccessTokenHash);

        return allowed
            ? new OrderResponse(order.Id, order.EventId, order.EventTitle, order.Email, order.PriceCents, order.BookedAt)
            : null;
    }

    /// <returns>The page, or null when the cursor is malformed.</returns>
    public async Task<OrderPage?> ListForUserAsync(int userId, PageQuery query, CancellationToken cancellationToken)
    {
        var orders = db.Orders.Where(o => o.UserId == userId);
        if (query.After is not null)
        {
            if (!PageCursor.TryDecode(query.After, CursorSort, out var key, out var id)
                || !DateTime.TryParse(key, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var bookedAt))
                return null;
            bookedAt = DateTime.SpecifyKind(bookedAt, DateTimeKind.Utc);
            orders = orders.Where(o => o.BookedAt < bookedAt || (o.BookedAt == bookedAt && o.Id < id));
        }

        // One extra row tells us whether another page exists.
        var rows = await orders
            .OrderByDescending(o => o.BookedAt).ThenByDescending(o => o.Id)
            .Take(query.Limit + 1)
            .Select(o => new OrderResponse(o.Id, o.EventId, o.Event.Title, o.Email, o.PriceCents, o.BookedAt, null))
            .ToListAsync(cancellationToken);

        var items = rows.Take(query.Limit).ToList();
        var nextCursor = rows.Count > query.Limit
            ? PageCursor.Encode(CursorSort, items[^1].BookedAt.ToString("O", CultureInfo.InvariantCulture), items[^1].Id)
            : null;
        return new OrderPage(items, nextCursor);
    }

    private static bool TokenMatches(string? token, string? storedHash) =>
        token is not null && storedHash is not null
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(SecureToken.Hash(token)), Encoding.UTF8.GetBytes(storedHash));
}
