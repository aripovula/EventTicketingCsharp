using EventTicketing.Api.Messaging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace EventTicketing.Tests.Messaging;

/// <summary>The local RabbitMQ, with exchange and queue names unique to one test; disposing deletes them.</summary>
public sealed class TestRabbitMq : IAsyncDisposable
{
    public TestRabbitMq()
    {
        var prefix = $"test-{Guid.NewGuid():N}";
        Names = new RabbitMqOptions
        {
            Exchange = $"{prefix}.ticketing",
            DeadLetterExchange = $"{prefix}.ticketing.dlx",
            BookingConfirmedQueue = $"{prefix}.booking-confirmed",
        };
    }

    public RabbitMqOptions Names { get; }

    public RabbitMqConnection Connection { get; } = new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:RabbitMq"] = "amqp://localhost:5672" })
        .Build());

    public async Task<IChannel> OpenChannelAsync()
    {
        var connection = await Connection.GetAsync(TestContext.Current.CancellationToken);
        return await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
    }

    public async Task<uint> MessageCountAsync(string queue)
    {
        await using var channel = await OpenChannelAsync();
        return await channel.MessageCountAsync(queue, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await using (var channel = await OpenChannelAsync())
        {
            await channel.QueueDeleteAsync(Names.BookingConfirmedQueue);
            await channel.QueueDeleteAsync(RabbitMqTopology.DeadLetterQueue(Names));
            await channel.ExchangeDeleteAsync(Names.Exchange);
            await channel.ExchangeDeleteAsync(Names.DeadLetterExchange);
        }

        await Connection.DisposeAsync();
    }
}
