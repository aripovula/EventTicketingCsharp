using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventTicketing.Api.Messaging;

/// <summary>
/// Handles booking confirmations (today it logs them; a confirmation email would be sent here).
/// Each message is acknowledged only after it's handled, so one in flight when the app dies
/// is redelivered to the next consumer.
/// </summary>
public class BookingConfirmationConsumer(
    RabbitMqConnection connection,
    IOptions<RabbitMqOptions> options,
    ILogger<BookingConfirmationConsumer> logger) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Retry until the broker is reachable; after that the client's automatic recovery
        // re-subscribes by itself whenever the connection drops.
        while (!stoppingToken.IsCancellationRequested)
        {
            IChannel? channel = null;
            try
            {
                var open = await connection.GetAsync(stoppingToken);
                channel = await open.CreateChannelAsync(cancellationToken: stoppingToken);
                await RabbitMqTopology.DeclareAsync(channel, options.Value, stoppingToken);
                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += (_, delivery) => HandleAsync(consumer.Channel, delivery);
                await channel.BasicConsumeAsync(options.Value.BookingConfirmedQueue, autoAck: false, consumer, stoppingToken);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Can't consume booking confirmations yet; retrying in {RetryDelay}", RetryDelay);
                await Task.Delay(RetryDelay, stoppingToken);
            }
            finally
            {
                if (channel is not null)
                    await channel.DisposeAsync();
            }
        }
    }

    private async Task HandleAsync(IChannel channel, BasicDeliverEventArgs delivery)
    {
        var message = JsonSerializer.Deserialize<BookingConfirmedMessage>(delivery.Body.Span)!;
        logger.LogInformation("Booking confirmed: order {OrderId} for event {EventId} ({EventTitle})",
            message.OrderId, message.EventId, message.EventTitle);
        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false);
    }
}
