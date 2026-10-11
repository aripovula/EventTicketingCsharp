using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EventTicketing.Tests.Integration;

/// <summary>The whole path against a real broker: booking → outbox → RabbitMQ → consumer.</summary>
[Trait("Category", "Integration")]
public class BookingMessagingIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private RabbitMqOptions Names => factory.Services.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

    private async Task<IChannel> OpenChannelAsync()
    {
        var connection = await factory.Services.GetRequiredService<RabbitMqConnection>().GetAsync(TestContext.Current.CancellationToken);
        return await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
    }

    private async Task<uint> MessageCountAsync(string queue)
    {
        await using var channel = await OpenChannelAsync();
        await RabbitMqTopology.DeclareAsync(channel, Names, TestContext.Current.CancellationToken);
        return await channel.MessageCountAsync(queue, TestContext.Current.CancellationToken);
    }

    // The outbox worker polls every 5 seconds, so allow a few polls.
    private static async Task EventuallyAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (!await condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Condition not met within 20 seconds");
            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    private async Task<bool> ConfirmationPublishedAsync(int orderId)
    {
        using var scope = factory.Services.CreateScope();
        var rows = await scope.ServiceProvider.GetRequiredService<AppDbContext>().OutboxMessages
            .Where(m => m.ProcessedAt != null)
            .ToListAsync(TestContext.Current.CancellationToken);
        return rows.Any(m => JsonSerializer.Deserialize<BookingConfirmedMessage>(m.Payload)!.OrderId == orderId);
    }

    [Fact]
    public async Task Booking_IsPublishedAndConsumed()
    {
        // Both tests share this class's queues, so compare with the dead-letter count before booking.
        var deadBefore = await MessageCountAsync(RabbitMqTopology.DeadLetterQueue(Names));
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/v1/events/1/bookings", new { email = "guest@example.com" }, TestContext.Current.CancellationToken);
        var orderId = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.GetProperty("data").GetProperty("id").GetInt32();

        await EventuallyAsync(() => ConfirmationPublishedAsync(orderId));
        await EventuallyAsync(async () => await MessageCountAsync(Names.BookingConfirmedQueue) == 0);

        Assert.Equal(deadBefore, await MessageCountAsync(RabbitMqTopology.DeadLetterQueue(Names)));
    }

    [Fact]
    public async Task UnreadableMessage_IsDeadLettered()
    {
        var deadBefore = await MessageCountAsync(RabbitMqTopology.DeadLetterQueue(Names));
        await using (var channel = await OpenChannelAsync())
        {
            await channel.BasicPublishAsync(Names.Exchange, BookingConfirmedMessage.RoutingKey,
                Encoding.UTF8.GetBytes("not json"), TestContext.Current.CancellationToken);
        }

        await EventuallyAsync(async () => await MessageCountAsync(RabbitMqTopology.DeadLetterQueue(Names)) == deadBefore + 1);
    }
}
