namespace EventTicketing.DemoLauncher;

public enum StackState
{
    /// <summary>No demo exists.</summary>
    None,
    Deploying,
    /// <summary>Every resource is deployed (the app may still be booting).</summary>
    Deployed,
    Failed,
    Deleting,
}

/// <summary>The demo's resources, managed as one deployment stack. Start* calls return without waiting.</summary>
public interface IDemoStack
{
    Task<StackState> GetStateAsync(CancellationToken cancellationToken);

    Task StartCreateAsync(CancellationToken cancellationToken);

    Task StartDeleteAsync(CancellationToken cancellationToken);
}
