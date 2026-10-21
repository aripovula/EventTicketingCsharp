namespace EventTicketing.DemoLauncher;

/// <summary>When the demo should go away: the only state the launcher keeps itself.</summary>
public interface IExpiryStore
{
    Task<DateTimeOffset?> GetAsync(CancellationToken cancellationToken);

    Task SetAsync(DateTimeOffset expiresAt, CancellationToken cancellationToken);
}
