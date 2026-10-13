using System.Diagnostics;
using EventTicketing.Api.Messaging;

namespace EventTicketing.Tests.Fakes;

public class FakeMessagePublisher : IMessagePublisher
{
    public List<(string Exchange, string RoutingKey, string Type, string Payload)> Published { get; } = [];

    // The trace each message was published in, keyed by its payload.
    public Dictionary<string, ActivityTraceId?> TraceIds { get; } = [];

    // When set, every publish throws it, as if the broker were down.
    public Exception? FailWith { get; set; }

    public Task PublishAsync(string exchange, string routingKey, string type, string payload, CancellationToken cancellationToken)
    {
        if (FailWith is not null)
            throw FailWith;
        Published.Add((exchange, routingKey, type, payload));
        TraceIds[payload] = Activity.Current?.TraceId;
        return Task.CompletedTask;
    }
}
