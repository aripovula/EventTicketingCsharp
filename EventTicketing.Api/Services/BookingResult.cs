using EventTicketing.Api.Contracts;

namespace EventTicketing.Api.Services;

public enum BookingStatus
{
    Booked,
    SoldOut,
    EventNotFound,
    EventArchived,
    PaymentDeclined,
    PaymentMethodRequired,
    NoSavedPaymentMethod,
}

/// <param name="Error">Why the card was declined, safe to show the buyer.</param>
public record BookingResult(BookingStatus Status, OrderResponse? Order = null, string? Error = null);
