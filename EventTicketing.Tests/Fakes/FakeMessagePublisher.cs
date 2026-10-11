using EventTicketing.Api.Messaging;

namespace EventTicketing.Tests.Fakes;

public class FakeMessagePublisher : IMessagePublisher
{
    public List<(string Exchange, string RoutingKey, string Type, string Payload)> Published { get; } = [];

    // When set, every publish throws it, as if the broker were down.
    public Exception? FailWith { get; set; }

    public Task PublishAsync(string exchange, string routingKey, string type, string payload, CancellationToken cancellationToken)
    {
        if (FailWith is not null)
            throw FailWith;
        Published.Add((exchange, routingKey, type, payload));
        return Task.CompletedTask;
    }
}
