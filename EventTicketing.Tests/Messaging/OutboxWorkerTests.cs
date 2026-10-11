using EventTicketing.Api.Data;
using EventTicketing.Api.Messaging;
using EventTicketing.Api.Models;
using EventTicketing.Tests.Fakes;
using EventTicketing.Tests.Integration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventTicketing.Tests.Messaging;

[Trait("Category", "Integration")]
public class OutboxWorkerTests(OutboxWorkerTests.HostWithoutWorker factory) : IClassFixture<OutboxWorkerTests.HostWithoutWorker>
{
    /// <summary>The app's own hosted worker would race these tests for the same rows, so this host runs without it.</summary>
    public class HostWithoutWorker : IntegrationTestFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
                services.Remove(services.Single(d => d.ImplementationType == typeof(OutboxWorker))));
        }
    }

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

    [Fact]
    public async Task ProcessPending_WhenPublishFails_RecordsTheErrorAndSchedulesARetry()
    {
        var message = await AddMessageAsync();
        _publisher.FailWith = new InvalidOperationException("Broker unreachable");

        await ProcessPendingAsync();

        var saved = await ReloadAsync(message);
        Assert.Null(saved.ProcessedAt);
        Assert.Equal((1, "Broker unreachable"), (saved.Attempts, saved.LastError));
        Assert.InRange(saved.NextAttemptAt!.Value, DateTime.UtcNow.AddSeconds(3), DateTime.UtcNow.AddSeconds(6));
    }

    [Fact]
    public async Task ProcessPending_WaitsForTheRetryTime()
    {
        var message = await AddMessageAsync(m => (m.Attempts, m.NextAttemptAt) = (1, DateTime.UtcNow.AddMinutes(1)));

        await ProcessPendingAsync();

        Assert.DoesNotContain(_publisher.Published, p => p.Payload == message.Payload);
    }

    [Fact]
    public async Task ProcessPending_RetriesOnceTheRetryTimeHasPassed()
    {
        var message = await AddMessageAsync(m => (m.Attempts, m.NextAttemptAt) = (1, DateTime.UtcNow.AddSeconds(-1)));

        await ProcessPendingAsync();

        var saved = await ReloadAsync(message);
        Assert.Equal(2, saved.Attempts);
        Assert.NotNull(saved.ProcessedAt);
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    [InlineData(4, 40)]
    public void RetryDelay_DoublesAfterEachFailure(int attempts, int seconds) =>
        Assert.Equal(TimeSpan.FromSeconds(seconds), OutboxWorker.RetryDelay(attempts));

    [Fact]
    public async Task ProcessPending_ParksAMessageAfterItsLastFailedAttempt()
    {
        var message = await AddMessageAsync(m => m.Attempts = OutboxWorker.MaxAttempts - 1);
        _publisher.FailWith = new InvalidOperationException("Broker unreachable");
        await ProcessPendingAsync();
        await CreateDb().OutboxMessages.Where(m => m.Id == message.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.NextAttemptAt, DateTime.UtcNow.AddSeconds(-1)), TestContext.Current.CancellationToken);
        _publisher.FailWith = null;

        await ProcessPendingAsync();

        Assert.DoesNotContain(_publisher.Published, p => p.Payload == message.Payload);
        var saved = await ReloadAsync(message);
        Assert.Equal(OutboxWorker.MaxAttempts, saved.Attempts);
        Assert.Null(saved.ProcessedAt);
    }
}
