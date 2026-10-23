using EventTicketing.DemoLauncher;

namespace EventTicketing.DemoLauncher.Tests.Fakes;

/// <summary>Records what the launcher asked Azure to do; tests set <see cref="State"/> directly.</summary>
public class FakeDemoStack : IDemoStack
{
    public const string Url = "https://eventticketing-demo.eastus.cloudapp.azure.com";

    public StackState State { get; set; } = StackState.None;
    public int Creates { get; private set; }
    public int Deletes { get; private set; }

    public Task<StackInfo> GetAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new StackInfo(State, State == StackState.None ? null : Url));

    public Task<string> StartCreateAsync(CancellationToken cancellationToken)
    {
        Creates++;
        State = StackState.Deploying;
        return Task.FromResult(Url);
    }

    public Task StartDeleteAsync(CancellationToken cancellationToken)
    {
        Deletes++;
        State = StackState.Deleting;
        return Task.CompletedTask;
    }
}
