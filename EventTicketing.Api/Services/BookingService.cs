using EventTicketing.Api.Contracts;

namespace EventTicketing.Api.Services;

public class BookingService
{
    public Task<BookingResult> BookAsync(int eventId, BookingRequest request, int? userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new BookingResult(BookingStatus.Booked));
    }
}
