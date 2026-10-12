using EventTicketing.Api.Data;
using EventTicketing.Api.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EventTicketing.Tests.Integration;

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Each test host would otherwise watch its appsettings files, and dozens of hosts exhaust
    // Linux's per-user inotify limit (128 by default). Tests never change config files mid-run.
    static IntegrationTestFactory() =>
        Environment.SetEnvironmentVariable("DOTNET_hostBuilder__reloadConfigOnChange", "false");

    private readonly string _connectionString =
        $"Host=localhost;Database=event_ticketing_test_{Guid.NewGuid():N}";

    private readonly string _rabbitMqPrefix = $"test-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-at-least-32-bytes");
        // Never call Stripe from tests; CI has no Stripe keys.
        builder.UseSetting("Payments:Provider", "Fake");
        // Each test host gets its own Redis key prefix, as it gets its own database.
        // Every TestServer request shares one client address, so lift the limits; rate-limit tests lower them.
        builder.UseSetting("RateLimiting:GlobalPermitLimit", "100000");
        builder.UseSetting("RateLimiting:BookingPermitLimit", "100000");
        builder.UseSetting("Redis:InstanceName", $"event-ticketing-test-{Guid.NewGuid():N}:");
        // Likewise its own RabbitMQ exchanges and queues, so hosts never consume each other's messages.
        builder.UseSetting("RabbitMq:Exchange", $"{_rabbitMqPrefix}.ticketing");
        builder.UseSetting("RabbitMq:DeadLetterExchange", $"{_rabbitMqPrefix}.ticketing.dlx");
        builder.UseSetting("RabbitMq:BookingConfirmedQueue", $"{_rabbitMqPrefix}.booking-confirmed");
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

        await DeleteRabbitMqTopologyAsync();

        await base.DisposeAsync();
    }

    private async Task DeleteRabbitMqTopologyAsync()
    {
        var names = Services.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
        var connection = await Services.GetRequiredService<RabbitMqConnection>().GetAsync(CancellationToken.None);
        await using var channel = await connection.CreateChannelAsync();
        // Deleting is idempotent, so hosts that never touched the broker are fine too.
        await channel.QueueDeleteAsync(names.BookingConfirmedQueue);
        await channel.QueueDeleteAsync(RabbitMqTopology.DeadLetterQueue(names));
        await channel.ExchangeDeleteAsync(names.Exchange);
        await channel.ExchangeDeleteAsync(names.DeadLetterExchange);
    }
}
