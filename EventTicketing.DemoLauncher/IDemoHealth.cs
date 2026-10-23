namespace EventTicketing.DemoLauncher;

/// <summary>Whether the deployed demo answers yet (cloud-init and Caddy's certificate take a few minutes).</summary>
public interface IDemoHealth
{
    Task<bool> IsUpAsync(string url, CancellationToken cancellationToken);
}
