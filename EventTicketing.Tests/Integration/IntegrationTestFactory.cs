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
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-at-least-32-bytes");
        // Each test host gets its own Redis key prefix, as it gets its own database.
        // Every TestServer request shares one client address, so lift the limits; rate-limit tests lower them.
        builder.UseSetting("RateLimiting:GlobalPermitLimit", "100000");
        builder.UseSetting("RateLimiting:BookingPermitLimit", "100000");
        builder.UseSetting("Redis:InstanceName", $"event-ticketing-test-{Guid.NewGuid():N}:");
    }

    public ValueTask InitializeAsync()
    {
        // Starting the host applies migrations, which creates the throwaway database.
        _ = Services;
        return ValueTask.CompletedTask;
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
