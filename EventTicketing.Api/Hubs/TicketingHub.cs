using Microsoft.AspNetCore.SignalR;

namespace EventTicketing.Api.Hubs;

/// <summary>
/// Real-time hub at /hubs/ticketing. Clients only listen: the server pushes BookingMade, EventCreated,
/// EventUpdated and EventDeleted (each with the event id) so open pages can refetch what changed.
/// </summary>
public class TicketingHub : Hub;
