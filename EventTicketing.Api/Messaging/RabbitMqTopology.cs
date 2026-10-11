using RabbitMQ.Client;

namespace EventTicketing.Api.Messaging;

public static class RabbitMqTopology
{
    /// <summary>
    /// Declares the exchange and queues. Declaring is idempotent, so the publisher and the
    /// consumer both run it and neither depends on the other having started first.
    /// </summary>
    public static async Task DeclareAsync(IChannel channel, RabbitMqOptions names, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(names.Exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        // Messages a consumer rejects go to the dead-letter exchange and wait in a ".dead" queue for a person.
        await channel.ExchangeDeclareAsync(names.DeadLetterExchange, ExchangeType.Fanout, durable: true, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(DeadLetterQueue(names), durable: true, exclusive: false, autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(DeadLetterQueue(names), names.DeadLetterExchange, routingKey: "", cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(names.BookingConfirmedQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = names.DeadLetterExchange },
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(names.BookingConfirmedQueue, names.Exchange, BookingConfirmedMessage.RoutingKey,
            cancellationToken: cancellationToken);
    }

    public static string DeadLetterQueue(RabbitMqOptions names) => $"{names.BookingConfirmedQueue}.dead";
}
