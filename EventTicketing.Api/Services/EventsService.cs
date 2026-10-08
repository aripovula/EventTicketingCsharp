using EventTicketing.Api.Contracts;

namespace EventTicketing.Api.Services;

public class EventsService
{
    public Task<EventPage> ListAsync(EventListQuery query, CancellationToken cancellationToken)
    {
        return Task.FromResult(new EventPage([], null));
    }
}
