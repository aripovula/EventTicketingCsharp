using EventTicketing.DemoLauncher.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace EventTicketing.DemoLauncher.Tests;

public class DemoLifecycleTests
{
    private static readonly DateTimeOffset NineTen = new(2026, 10, 21, 9, 10, 0, TimeSpan.Zero);

    private readonly FakeDemoStack _stack = new();
    private readonly InMemoryExpiryStore _expiry = new();
    private readonly FakeDemoHealth _health = new();
    private readonly FakeTimeProvider _time = new(NineTen);
    private readonly DemoLifecycle _demo;

    public DemoLifecycleTests()
    {
        var options = Options.Create(new DemoOptions { DnsLabel = "eventticketing-demo", Location = "eastus", LifetimeMinutes = 30 });
        _demo = new DemoLifecycle(_stack, _expiry, _health, _time, options, NullLogger<DemoLifecycle>.Instance);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task FirstRequest_StartsTheDemoForThirtyMinutes()
    {
        var status = await _demo.RequestAsync(Ct);

        Assert.Equal(1, _stack.Creates);
        Assert.Equal(NineTen.AddMinutes(30), _expiry.ExpiresAt);
        Assert.Equal(new DemoStatus(DemoStates.Starting, "https://eventticketing-demo.eastus.cloudapp.azure.com", NineTen.AddMinutes(30)), status);
    }

    [Fact]
    public async Task ALaterRequest_ExtendsTheSameDemo()
    {
        await _demo.RequestAsync(Ct);
        _stack.State = StackState.Deployed;
        _time.Advance(TimeSpan.FromMinutes(15));

        await _demo.RequestAsync(Ct);

        Assert.Equal(1, _stack.Creates);
        Assert.Equal(NineTen.AddMinutes(45), _expiry.ExpiresAt);
    }

    [Fact]
    public async Task ARequestWhileItIsStillDeploying_DoesNotStartASecondOne()
    {
        await _demo.RequestAsync(Ct);

        await _demo.RequestAsync(Ct);

        Assert.Equal(1, _stack.Creates);
    }

    [Fact]
    public async Task ARequestAfterAFailedDeployment_TriesAgain()
    {
        _stack.State = StackState.Failed;

        await _demo.RequestAsync(Ct);

        Assert.Equal(1, _stack.Creates);
    }

    [Fact]
    public async Task ARequestWhileItIsBeingDeleted_OnlyExtends_AndTheTimerRestartsIt()
    {
        _stack.State = StackState.Deleting;

        var status = await _demo.RequestAsync(Ct);

        Assert.Equal(DemoStates.Stopping, status.State);
        Assert.Equal(0, _stack.Creates);

        _stack.State = StackState.None;
        await _demo.ReconcileAsync(Ct);

        Assert.Equal(1, _stack.Creates);
    }

    [Theory]
    [InlineData(false, DemoStates.Starting)]
    [InlineData(true, DemoStates.Ready)]
    public async Task ADeployedDemo_IsReadyOnlyOnceItAnswers(bool up, string expected)
    {
        _stack.State = StackState.Deployed;
        _health.Up = up;

        var status = await _demo.GetStatusAsync(Ct);

        Assert.Equal(expected, status.State);
    }

    [Fact]
    public async Task GettingTheStatus_ChangesNothing()
    {
        var status = await _demo.GetStatusAsync(Ct);

        Assert.Equal(DemoStates.Stopped, status.State);
        Assert.Null(_expiry.ExpiresAt);
        Assert.Equal(0, _stack.Creates);
    }

    [Fact]
    public async Task TheTimer_LeavesAnUnexpiredDemoRunning()
    {
        await _demo.RequestAsync(Ct);
        _stack.State = StackState.Deployed;
        _time.Advance(TimeSpan.FromMinutes(29));

        await _demo.ReconcileAsync(Ct);

        Assert.Equal(0, _stack.Deletes);
    }

    [Fact]
    public async Task TheTimer_DeletesTheDemoOnceNobodyAskedForThirtyMinutes()
    {
        await _demo.RequestAsync(Ct);
        _stack.State = StackState.Deployed;
        _time.Advance(TimeSpan.FromMinutes(30));

        await _demo.ReconcileAsync(Ct);

        Assert.Equal(1, _stack.Deletes);
    }

    [Fact]
    public async Task TheTimer_WaitsForADeploymentToFinishBeforeDeleting()
    {
        await _demo.RequestAsync(Ct);
        _time.Advance(TimeSpan.FromMinutes(31));

        await _demo.ReconcileAsync(Ct);

        Assert.Equal(0, _stack.Deletes);
    }

    [Fact]
    public async Task TheTimer_DeletesAnExpiredFailedDeployment()
    {
        await _demo.RequestAsync(Ct);
        _stack.State = StackState.Failed;
        _time.Advance(TimeSpan.FromMinutes(30));

        await _demo.ReconcileAsync(Ct);

        Assert.Equal(1, _stack.Deletes);
        Assert.Equal(1, _stack.Creates);
    }

    [Fact]
    public async Task TheTimer_DoesNothingWhenNoDemoWasEverRequested()
    {
        await _demo.ReconcileAsync(Ct);

        Assert.Equal(0, _stack.Creates);
        Assert.Equal(0, _stack.Deletes);
    }
}
