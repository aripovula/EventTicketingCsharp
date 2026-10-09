using EventTicketing.Api.Contracts;

namespace EventTicketing.Api.Services;

public record OrderPage(IReadOnlyList<OrderResponse> Items, string? NextCursor);
