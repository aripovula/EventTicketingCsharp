namespace EventTicketing.Api.Contracts;

public record EventResponse(
    int Id,
    string Title,
    string Description,
    DateTime StartTime,
    DateTime EndTime,
    string Venue,
    string EventType,
    int TotalSeats,
    int AvailableSeats,
    int PriceCents,
    string? ImageUrl);
