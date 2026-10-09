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

    private static bool TokenMatches(string? token, string? storedHash) =>
        token is not null && storedHash is not null
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(SecureToken.Hash(token)), Encoding.UTF8.GetBytes(storedHash));
}
