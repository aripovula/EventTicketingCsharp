using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class TicketingHubIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Negotiate_AnonymousClient_OffersWebSockets()
    {
        var response = await factory.CreateClient().PostAsync("/hubs/ticketing/negotiate?negotiateVersion=1", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("WebSockets", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private async Task<HubConnection> ConnectAsync()
    {
        // TestServer has no real sockets, so the client long-polls through its in-memory handler.
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "hubs/ticketing"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
        await connection.StartAsync(TestContext.Current.CancellationToken);
        return connection;
    }

    private async Task<int> CreateEventAsync(int availableSeats = 10)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ev = new Event
        {
            Title = "Hub Event",
            Description = "For hub tests.",
            StartTime = DateTime.UtcNow.AddDays(5),
            EndTime = DateTime.UtcNow.AddDays(5).AddHours(2),
            Venue = "Test Hall",
            TotalSeats = 10,
            AvailableSeats = availableSeats,
            PriceCents = 2500,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return ev.Id;
    }

    private async Task<HttpClient> AdminAsync()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "admin@example.com", password = "Password" }, TestContext.Current.CancellationToken);
        return client;
    }

    private static object NewEventRequest() => new
    {
        title = "Hub Created Event",
        description = "Created in a hub test.",
        startTime = DateTime.UtcNow.AddDays(8),
        endTime = DateTime.UtcNow.AddDays(8).AddHours(2),
        venue = "Test Hall",
        eventType = "Other",
        totalSeats = 10,
        availableSeats = 10,
        priceCents = 1000,
    };

    [Fact]
    public async Task Booking_PushesBookingMadeWithTheEventId()
    {
        var eventId = await CreateEventAsync();
        await using var connection = await ConnectAsync();
        var received = new TaskCompletionSource<int>();
        connection.On<int>("BookingMade", received.SetResult);

        await factory.CreateClient().PostAsJsonAsync($"/api/v1/events/{eventId}/bookings", new { email = "guest@example.com" }, TestContext.Current.CancellationToken);

        Assert.Equal(eventId, await received.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SoldOutBooking_PushesNothing()
    {
        var eventId = await CreateEventAsync(availableSeats: 0);
        await using var connection = await ConnectAsync();
        var received = new TaskCompletionSource<int>();
        connection.On<int>("BookingMade", received.SetResult);

        var response = await factory.CreateClient().PostAsJsonAsync($"/api/v1/events/{eventId}/bookings", new { email = "guest@example.com" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await Task.Delay(500, TestContext.Current.CancellationToken);
        Assert.False(received.Task.IsCompleted);
    }

    [Fact]
    public async Task CreatingAnEvent_PushesEventCreatedWithItsId()
    {
        await using var connection = await ConnectAsync();
        var received = new TaskCompletionSource<int>();
        connection.On<int>("EventCreated", received.SetResult);

        var response = await (await AdminAsync()).PostAsJsonAsync("/api/v1/events", NewEventRequest(), TestContext.Current.CancellationToken);

        var id = (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("data").GetProperty("id").GetInt32();
        Assert.Equal(id, await received.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdatingAnEvent_PushesEventUpdatedWithItsId()
    {
        var eventId = await CreateEventAsync();
        await using var connection = await ConnectAsync();
        var received = new TaskCompletionSource<int>();
        connection.On<int>("EventUpdated", received.SetResult);

        await (await AdminAsync()).PutAsJsonAsync($"/api/v1/events/{eventId}", NewEventRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(eventId, await received.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeletingAnEvent_PushesEventDeletedWithItsId()
    {
        var eventId = await CreateEventAsync();
        await using var connection = await ConnectAsync();
        var received = new TaskCompletionSource<int>();
        connection.On<int>("EventDeleted", received.SetResult);

        await (await AdminAsync()).DeleteAsync($"/api/v1/events/{eventId}", TestContext.Current.CancellationToken);

        Assert.Equal(eventId, await received.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }
}
