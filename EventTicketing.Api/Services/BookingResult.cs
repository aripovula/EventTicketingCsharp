using EventTicketing.Api.Contracts;

namespace EventTicketing.Api.Services;

public enum BookingStatus
{
    Booked,
    SoldOut,
    EventNotFound,
}

public record BookingResult(BookingStatus Status, OrderResponse? Order = null);
