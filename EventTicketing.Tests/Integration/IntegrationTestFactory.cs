using EventTicketing.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Integration;

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _connectionString =
        $"Host=localhost;Database=event_ticketing_test_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
    }

    public async ValueTask InitializeAsync()
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        using (var scope = Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
        }

        await base.DisposeAsync();
    }
}
