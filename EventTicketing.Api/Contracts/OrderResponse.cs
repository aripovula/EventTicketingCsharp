namespace EventTicketing.Api.Contracts;

public record OrderResponse(int Id, int EventId, string EventTitle, string Email, int PriceCents, DateTime BookedAt);
