using RabbitMQ.Client;

namespace EventTicketing.Api.Messaging;

/// <summary>
/// The app's one long-lived RabbitMQ connection, opened on first use. Channels are cheap and
/// created per job; the client reconnects on its own after a network failure.
/// </summary>
public sealed class RabbitMqConnection(IConfiguration configuration) : IAsyncDisposable
{
    private readonly SemaphoreSlim _openLock = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
            return _connection;

        await _openLock.WaitAsync(cancellationToken);
        try
        {
            // A failed open leaves the field null, so the next caller tries again.
            return _connection ??= await new ConnectionFactory
            {
                Uri = new Uri(configuration.GetConnectionString("RabbitMq")!),
                ClientProvidedName = "event-ticketing-api",
            }.CreateConnectionAsync(cancellationToken);
        }
        finally
        {
            _openLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
        _openLock.Dispose();
    }
}
