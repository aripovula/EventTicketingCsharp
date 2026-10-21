using EventTicketing.DemoLauncher;

namespace EventTicketing.DemoLauncher.Tests.Fakes;

public class FakeDemoHealth : IDemoHealth
{
    public bool Up { get; set; }

    public Task<bool> IsUpAsync(CancellationToken cancellationToken) => Task.FromResult(Up);
}
