using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventTicketing.DemoLauncher;

/// <summary>
/// Keeps at most one demo running. Every request pushes its end to now + the lifetime, starting it if
/// none is up; <see cref="ReconcileAsync"/> runs on a timer and deletes it once nobody has asked for a
/// whole lifetime (or starts it, if a request came in while the previous one was being deleted).
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

    public async Task<DemoStatus> GetStatusAsync(CancellationToken cancellationToken) =>
        await DescribeAsync(await stack.GetStateAsync(cancellationToken), await expiry.GetAsync(cancellationToken), cancellationToken);

    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        var expiresAt = await expiry.GetAsync(cancellationToken);
        var wanted = expiresAt > time.GetUtcNow();
        var state = await stack.GetStateAsync(cancellationToken);

        // A stack that is still deploying or deleting can't be changed; the next run deals with it.
        if (!wanted && state is StackState.Deployed or StackState.Failed)
        {
            logger.LogInformation("Deleting the demo: it expired at {ExpiresAt}", expiresAt);
            await stack.StartDeleteAsync(cancellationToken);
        }
        else if (wanted && state is StackState.None or StackState.Failed)
        {
            logger.LogInformation("Starting the demo (stack was {State}): wanted until {ExpiresAt}", state, expiresAt);
            await stack.StartCreateAsync(cancellationToken);
        }
    }

    private async Task<DemoStatus> DescribeAsync(StackState state, DateTimeOffset? expiresAt, CancellationToken cancellationToken)
    {
        var shown = state switch
        {
            StackState.None => DemoStates.Stopped,
            StackState.Deleting => DemoStates.Stopping,
            StackState.Deployed when await health.IsUpAsync(cancellationToken) => DemoStates.Ready,
            // Failed deployments are retried by the next request or timer run.
            _ => DemoStates.Starting,
        };
        return new DemoStatus(shown, options.Value.Url, expiresAt);
    }
}
