using System.Text;
using EventTicketing.Api.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EventTicketing.Tests.Messaging;

[Trait("Category", "Integration")]
public sealed class RabbitMqPublisherTests : IAsyncDisposable
{
    private readonly TestRabbitMq _rabbitMq = new();

    public ValueTask DisposeAsync() => _rabbitMq.DisposeAsync();

    private RabbitMqPublisher CreatePublisher() => new(_rabbitMq.Connection, Options.Create(_rabbitMq.Names));

    [Fact]
    public async Task Publish_DeliversAPersistentMessageToTheBookingQueue()
    {
        await CreatePublisher().PublishAsync(_rabbitMq.Names.Exchange, BookingConfirmedMessage.RoutingKey,
            "BookingConfirmedMessage", """{"OrderId":1}""", TestContext.Current.CancellationToken);

        await using var channel = await _rabbitMq.OpenChannelAsync();
        var delivery = await channel.BasicGetAsync(_rabbitMq.Names.BookingConfirmedQueue, autoAck: true, TestContext.Current.CancellationToken);
        Assert.NotNull(delivery);
        Assert.Equal("""{"OrderId":1}""", Encoding.UTF8.GetString(delivery.Body.Span));
        Assert.Equal(("BookingConfirmedMessage", DeliveryModes.Persistent), (delivery.BasicProperties.Type, delivery.BasicProperties.DeliveryMode));
    }

    [Fact]
    public async Task Publish_ThatNoQueueReceives_Throws()
    {
        await Assert.ThrowsAsync<PublishReturnException>(() => CreatePublisher().PublishAsync(
            _rabbitMq.Names.Exchange, "nobody.listens", "Test", "{}", TestContext.Current.CancellationToken));
    }
}
