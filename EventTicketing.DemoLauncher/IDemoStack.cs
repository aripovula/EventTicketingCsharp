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

/// <summary>The stack's state and, while it exists, the address of the demo it holds.</summary>
public record StackInfo(StackState State, string? Url);

/// <summary>The demo's resources, managed as one deployment stack. Start* calls return without waiting.</summary>
public interface IDemoStack
{
    Task<StackInfo> GetAsync(CancellationToken cancellationToken);

    /// <summary>Starts deploying a new demo and returns the address it will have.</summary>
    Task<string> StartCreateAsync(CancellationToken cancellationToken);

    Task StartDeleteAsync(CancellationToken cancellationToken);
}
