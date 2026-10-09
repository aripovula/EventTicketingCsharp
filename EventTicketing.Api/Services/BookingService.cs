using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
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

        return new BookingResult(BookingStatus.Booked);
    }
}
