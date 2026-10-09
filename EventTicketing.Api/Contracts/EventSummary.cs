namespace EventTicketing.Api.Contracts;

public record EventSummary(int EventId, string Title, int TotalSeats, int SoldSeats, int RemainingSeats, int RevenueCents);
