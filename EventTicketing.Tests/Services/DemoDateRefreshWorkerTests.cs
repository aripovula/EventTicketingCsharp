using EventTicketing.Api.Data;
using EventTicketing.Api.Services;
using EventTicketing.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventTicketing.Tests.Services;

[Trait("Category", "Integration")]
public class DemoDateRefreshWorkerTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private Task<DateTime> EarliestStartAsync() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>()
            .Events.MinAsync(e => e.StartTime, TestContext.Current.CancellationToken);

    [Fact]
    public async Task RefreshesTheDemoAsSoonAsItStarts()
    {
        await factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>().Events
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.StartTime, e => e.StartTime.AddDays(-40))
                .SetProperty(e => e.EndTime, e => e.EndTime.AddDays(-40)), TestContext.Current.CancellationToken);
        var worker = new DemoDateRefreshWorker(factory.Services.GetRequiredService<IServiceScopeFactory>(), NullLogger<DemoDateRefreshWorker>.Instance);

        await worker.StartAsync(TestContext.Current.CancellationToken);

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (await EarliestStartAsync() <= DateTime.UtcNow)
        {
            Assert.True(DateTime.UtcNow < deadline, "The demo wasn't refreshed within 10 seconds");
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }
        await worker.StopAsync(TestContext.Current.CancellationToken);
    }
}
