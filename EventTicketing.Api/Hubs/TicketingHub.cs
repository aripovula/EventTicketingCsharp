using Microsoft.AspNetCore.SignalR;

namespace EventTicketing.Api.Hubs;

/// <summary>
/// Real-time hub at /hubs/ticketing. Clients only listen: the server pushes BookingMade, EventCreated,
/// EventUpdated and EventDeleted (each with the event id) so open pages can refetch what changed.
/// </summary>
public class TicketingHub : Hub;

/// <summary>Names of the messages TicketingHub clients receive.</summary>
public static class TicketingEvents
{
    public const string BookingMade = nameof(BookingMade);
    public const string EventCreated = nameof(EventCreated);
    public const string EventUpdated = nameof(EventUpdated);
    public const string EventDeleted = nameof(EventDeleted);
    public const string EventArchived = nameof(EventArchived);
}
