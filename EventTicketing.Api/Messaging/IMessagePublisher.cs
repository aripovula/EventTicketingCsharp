namespace EventTicketing.Api.Messaging;

public interface IMessagePublisher
{
    /// <summary>Publishes a JSON payload; completes once the broker has confirmed it and throws if it can't.</summary>
    Task PublishAsync(string exchange, string routingKey, string type, string payload, CancellationToken cancellationToken);
}
