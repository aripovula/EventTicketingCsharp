using EventTicketing.Api.Contracts;

namespace EventTicketing.Api.Services;

public record EventPage(IReadOnlyList<EventResponse> Items, string? NextCursor);
