using System.Text.Json;
using EventTicketing.Api.Messaging;
using EventTicketing.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace EventTicketing.Tests.Messaging;

[Trait("Category", "Integration")]
public sealed class BookingConfirmationConsumerTests : IAsyncDisposable
{
    private readonly TestRabbitMq _rabbitMq = new();
    private readonly CollectingLogger<BookingConfirmationConsumer> _logger = new();

    public ValueTask DisposeAsync() => _rabbitMq.DisposeAsync();

    private async Task<BookingConfirmationConsumer> StartConsumerAsync()
    {
        var consumer = new BookingConfirmationConsumer(_rabbitMq.Connection, Options.Create(_rabbitMq.Names), _logger);
        await consumer.StartAsync(TestContext.Current.CancellationToken);
        return consumer;
    }

    private Task PublishAsync(string body) =>
        new RabbitMqPublisher(_rabbitMq.Connection, Options.Create(_rabbitMq.Names)).PublishAsync(
            _rabbitMq.Names.Exchange, BookingConfirmedMessage.RoutingKey, nameof(BookingConfirmedMessage), body,
            TestContext.Current.CancellationToken);

    private static async Task EventuallyAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!await condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Condition not met within 10 seconds");
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task HandlesAndAcknowledgesABookingConfirmation()
    {
        var consumer = await StartConsumerAsync();

        await PublishAsync(JsonSerializer.Serialize(new BookingConfirmedMessage(41, 7, "Jazz Night", "guest@example.com", 2500, DateTime.UtcNow)));

        await EventuallyAsync(() => Task.FromResult(_logger.Messages.Any(m => m.Contains("order 41"))));
        await Task.Delay(500, TestContext.Current.CancellationToken);
        await consumer.StopAsync(TestContext.Current.CancellationToken);
        // Closing the consumer's channel would put an unacknowledged message back on the queue.
        Assert.Equal(0u, await _rabbitMq.MessageCountAsync(_rabbitMq.Names.BookingConfirmedQueue));
    }

    [Fact]
    public async Task DeadLettersAnUnreadableMessage()
    {
        var consumer = await StartConsumerAsync();

        await PublishAsync("not json");

        await EventuallyAsync(async () => await _rabbitMq.MessageCountAsync(RabbitMqTopology.DeadLetterQueue(_rabbitMq.Names)) == 1);
        await consumer.StopAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0u, await _rabbitMq.MessageCountAsync(_rabbitMq.Names.BookingConfirmedQueue));
    }
}
