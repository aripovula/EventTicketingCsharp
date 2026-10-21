using EventTicketing.DemoLauncher;

namespace EventTicketing.DemoLauncher.Tests.Fakes;

public class InMemoryExpiryStore : IExpiryStore
{
    public DateTimeOffset? ExpiresAt { get; set; }

    public Task<DateTimeOffset?> GetAsync(CancellationToken cancellationToken) => Task.FromResult(ExpiresAt);

    public Task SetAsync(DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        ExpiresAt = expiresAt;
        return Task.CompletedTask;
    }
}
