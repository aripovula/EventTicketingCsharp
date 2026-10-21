namespace EventTicketing.DemoLauncher;

/// <summary>What the portfolio page shows: <see cref="DemoStates"/>, the demo's address, and when it ends.</summary>
public record DemoStatus(string State, string Url, DateTimeOffset? ExpiresAt);

public static class DemoStates
{
    public const string Stopped = "stopped";
    public const string Starting = "starting";
    public const string Ready = "ready";
    public const string Stopping = "stopping";
}
