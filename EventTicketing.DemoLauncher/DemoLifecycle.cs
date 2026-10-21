using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventTicketing.DemoLauncher;

/// <summary>
/// Keeps at most one demo running. Every request pushes its end to now + the lifetime, starting it if
/// none is up.
/// </summary>
public class DemoLifecycle(
    IDemoStack stack,
    IExpiryStore expiry,
    IDemoHealth health,
    TimeProvider time,
    IOptions<DemoOptions> options,
    ILogger<DemoLifecycle> logger)
{
    public async Task<DemoStatus> RequestAsync(CancellationToken cancellationToken)
    {
        var expiresAt = time.GetUtcNow().AddMinutes(options.Value.LifetimeMinutes);
        await expiry.SetAsync(expiresAt, cancellationToken);

        var state = await stack.GetStateAsync(cancellationToken);
        if (state is StackState.None or StackState.Failed)
        {
            logger.LogInformation("Starting the demo (stack was {State})", state);
            await stack.StartCreateAsync(cancellationToken);
            state = StackState.Deploying;
        }
        return await DescribeAsync(state, expiresAt, cancellationToken);
    }

    private async Task<DemoStatus> DescribeAsync(StackState state, DateTimeOffset? expiresAt, CancellationToken cancellationToken)
    {
        var shown = state switch
        {
            StackState.None => DemoStates.Stopped,
            StackState.Deleting => DemoStates.Stopping,
            StackState.Deployed when await health.IsUpAsync(cancellationToken) => DemoStates.Ready,
            // Failed deployments are retried by the next request.
            _ => DemoStates.Starting,
        };
        return new DemoStatus(shown, options.Value.Url, expiresAt);
    }
}
