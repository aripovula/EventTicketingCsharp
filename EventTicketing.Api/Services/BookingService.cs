using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Services;

public class BookingService(AppDbContext db)
{
    public async Task<BookingResult> BookAsync(int eventId, BookingRequest request, int? userId, CancellationToken cancellationToken)
    {
        // Check and decrement in ONE statement: the row lock queues concurrent buyers,
        // so the second one for the last seat matches zero rows instead of overselling.
        var reserved = await db.Events
            .Where(e => e.Id == eventId && e.AvailableSeats > 0)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.AvailableSeats, e => e.AvailableSeats - 1), cancellationToken);

        if (reserved == 0)
        {
            var exists = await db.Events.AnyAsync(e => e.Id == eventId, cancellationToken);
            return new BookingResult(exists ? BookingStatus.SoldOut : BookingStatus.EventNotFound);
        }

        var ev = await db.Events.SingleAsync(e => e.Id == eventId, cancellationToken);
        // Guests have no account to find the order by, so they get a one-time lookup secret.
        var guestToken = userId is null ? SecureToken.Generate() : null;
        var order = new Order
        {
            EventId = eventId,
            UserId = userId,
            Email = request.Email,
            PriceCents = ev.PriceCents,
            BookedAt = DateTime.UtcNow,
            GuestAccessTokenHash = guestToken is null ? null : SecureToken.Hash(guestToken),
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return new BookingResult(BookingStatus.Booked,
            new OrderResponse(order.Id, eventId, ev.Title, order.Email, order.PriceCents, order.BookedAt, guestToken));
    }
}
