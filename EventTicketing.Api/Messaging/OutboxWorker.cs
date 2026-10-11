using EventTicketing.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Messaging;

/// <summary>
/// Polls the outbox every few seconds and publishes pending messages to RabbitMQ. A message is
/// marked processed only after the broker confirms it, so delivery is at-least-once: a crash
/// between the publish and the save sends it again, and consumers must tolerate duplicates.
/// </summary>
public class OutboxWorker(IServiceScopeFactory scopeFactory, IMessagePublisher publisher, ILogger<OutboxWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;
    private static readonly TimeSpan FirstRetryDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // E.g. the database is briefly unreachable; the next poll tries again.
                logger.LogError(ex, "Outbox poll failed");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    /// <summary>Publishes one batch of pending messages, oldest first.</summary>
    public async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && (m.NextAttemptAt == null || m.NextAttemptAt <= DateTime.UtcNow))
            .OrderBy(m => m.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.Attempts++;
            try
            {
                await publisher.PublishAsync(message.Exchange, message.RoutingKey, message.Type, message.Payload, cancellationToken);
                message.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Back off exponentially (5s, 10s, 20s, ...) so a broker outage isn't hammered.
                message.LastError = ex.Message[..Math.Min(ex.Message.Length, 2000)];
                message.NextAttemptAt = DateTime.UtcNow + RetryDelay(message.Attempts);
                logger.LogWarning(ex, "Publishing outbox message {MessageId} failed (attempt {Attempts})", message.Id, message.Attempts);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public static TimeSpan RetryDelay(int attempts) => FirstRetryDelay * Math.Pow(2, attempts - 1);
}
