using EventTicketing.Api.Data;
using EventTicketing.Api.Messaging;
using EventTicketing.Api.Models;
using EventTicketing.Tests.Fakes;
using EventTicketing.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventTicketing.Tests.Messaging;

[Trait("Category", "Integration")]
public class OutboxWorkerTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly FakeMessagePublisher _publisher = new();

    private AppDbContext CreateDb() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    private Task ProcessPendingAsync() =>
        new OutboxWorker(factory.Services.GetRequiredService<IServiceScopeFactory>(), _publisher, NullLogger<OutboxWorker>.Instance)
            .ProcessPendingAsync(TestContext.Current.CancellationToken);

    private async Task<OutboxMessage> AddMessageAsync(Action<OutboxMessage>? configure = null)
    {
        var message = new OutboxMessage
        {
            Type = "TestMessage",
            Payload = $$"""{"id": "{{Guid.NewGuid():N}}"}""",
            Exchange = "test-exchange",
            RoutingKey = "test.key",
            CreatedAt = DateTime.UtcNow,
        };
        configure?.Invoke(message);
        var db = CreateDb();
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return message;
    }

    private Task<OutboxMessage> ReloadAsync(OutboxMessage message) =>
        CreateDb().OutboxMessages.SingleAsync(m => m.Id == message.Id, TestContext.Current.CancellationToken);

    [Fact]
    public async Task ProcessPending_PublishesAPendingMessageAndMarksItProcessed()
    {
        var message = await AddMessageAsync();

        await ProcessPendingAsync();

        Assert.Contains(("test-exchange", "test.key", "TestMessage", message.Payload), _publisher.Published);
        var saved = await ReloadAsync(message);
        Assert.NotNull(saved.ProcessedAt);
        Assert.Equal(1, saved.Attempts);
    }

    [Fact]
    public async Task ProcessPending_SkipsProcessedMessages()
    {
        var message = await AddMessageAsync(m => m.ProcessedAt = DateTime.UtcNow);

        await ProcessPendingAsync();

        Assert.DoesNotContain(_publisher.Published, p => p.Payload == message.Payload);
    }

    [Fact]
    public async Task ProcessPending_PublishesOldestFirst()
    {
        var first = await AddMessageAsync();
        var second = await AddMessageAsync();

        await ProcessPendingAsync();

        var order = _publisher.Published.Select(p => p.Payload).ToList();
        Assert.True(order.IndexOf(first.Payload) < order.IndexOf(second.Payload));
    }
}
