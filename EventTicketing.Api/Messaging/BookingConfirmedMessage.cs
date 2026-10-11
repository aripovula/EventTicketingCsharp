namespace EventTicketing.Api.Messaging;

/// <summary>Announces a new order; consumers (confirmation emails, analytics) bind their own queue to it.</summary>
public record BookingConfirmedMessage(
    int OrderId,
    int EventId,
    string EventTitle,
    string Email,
    int PriceCents,
    DateTime BookedAt)
{
    public const string RoutingKey = "booking.confirmed";
}
