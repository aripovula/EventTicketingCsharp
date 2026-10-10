namespace EventTicketing.Api.Contracts;

/// <param name="GuestAccessToken">
/// Only in the booking response of a guest booking: the secret that lets the guest view the order later.
/// </param>
public record OrderResponse(
    int Id,
    int EventId,
    string EventTitle,
    string EventVenue,
    string EventType,
    DateTime EventStartTime,
    DateTime EventEndTime,
    string Email,
    int PriceCents,
    DateTime BookedAt,
    string? GuestAccessToken = null);
