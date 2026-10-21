using EventTicketing.DemoLauncher;

namespace EventTicketing.DemoLauncher.Tests.Fakes;

/// <summary>Records what the launcher asked Azure to do; tests set <see cref="State"/> directly.</summary>
public class FakeDemoStack : IDemoStack
{
    public StackState State { get; set; } = StackState.None;
    public int Creates { get; private set; }
    public int Deletes { get; private set; }

    public Task<StackState> GetStateAsync(CancellationToken cancellationToken) => Task.FromResult(State);

    public Task StartCreateAsync(CancellationToken cancellationToken)
    {
        Creates++;
        State = StackState.Deploying;
        return Task.CompletedTask;
    }

    public Task StartDeleteAsync(CancellationToken cancellationToken)
    {
        Deletes++;
        State = StackState.Deleting;
        return Task.CompletedTask;
    }
}
