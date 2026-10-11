using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EventTicketing.Api.Messaging;

public class RabbitMqPublisher(RabbitMqConnection connection, IOptions<RabbitMqOptions> options) : IMessagePublisher
{
    // Publisher confirms make the publish await the broker's acknowledgement, and mandatory
    // publishing turns "no queue to route this to" into an exception instead of a silent drop.
    private static readonly CreateChannelOptions ConfirmedChannel =
        new(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);

    public async Task PublishAsync(string exchange, string routingKey, string type, string payload, CancellationToken cancellationToken)
    {
        var open = await connection.GetAsync(cancellationToken);
        await using var channel = await open.CreateChannelAsync(ConfirmedChannel, cancellationToken);
        await RabbitMqTopology.DeclareAsync(channel, options.Value, cancellationToken);

        var properties = new BasicProperties { Persistent = true, ContentType = "application/json", Type = type };
        await channel.BasicPublishAsync(exchange, routingKey, mandatory: true, properties,
            Encoding.UTF8.GetBytes(payload), cancellationToken);
    }
}
