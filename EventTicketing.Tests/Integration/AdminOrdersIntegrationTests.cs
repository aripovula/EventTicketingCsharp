using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class AdminOrdersIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private async Task<HttpClient> SignedInAsync(string email)
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Password" }, TestContext.Current.CancellationToken);
        return client;
    }

    [Fact]
    public async Task Orders_Anonymously_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/admin/orders", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Orders_AsRegularUser_Returns403()
    {
        var response = await (await SignedInAsync("john@example.com")).GetAsync("/api/v1/admin/orders", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Orders_AsAdmin_IncludesOtherUsersOrders()
    {
        int eventId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ev = new Event
            {
                Title = "Admin Orders Event",
                Description = "For admin order tests.",
                StartTime = DateTime.UtcNow.AddDays(5),
                EndTime = DateTime.UtcNow.AddDays(5).AddHours(2),
                Venue = "Test Hall",
                TotalSeats = 10,
                AvailableSeats = 10,
                PriceCents = 2500,
            };
            db.Events.Add(ev);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            eventId = ev.Id;
        }
        var booked = await (await SignedInAsync("jane@example.com")).PostAsJsonAsync(
            $"/api/v1/events/{eventId}/bookings", new { email = "jane@example.com", paymentMethodId = "pm_card_visa" }, TestContext.Current.CancellationToken);
        var orderId = JsonDocument.Parse(await booked.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var body = await (await SignedInAsync("admin@example.com")).GetStringAsync("/api/v1/admin/orders", TestContext.Current.CancellationToken);

        Assert.Contains(JsonDocument.Parse(body).RootElement.GetProperty("data").EnumerateArray(), o => o.GetProperty("id").GetInt32() == orderId);
    }

    private async Task<int> BookNewEventAsync(string title)
    {
        int eventId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ev = new Event
            {
                Title = title,
                Description = "For admin order filter tests.",
                StartTime = DateTime.UtcNow.AddDays(6),
                EndTime = DateTime.UtcNow.AddDays(6).AddHours(2),
                Venue = "Test Hall",
                TotalSeats = 10,
                AvailableSeats = 10,
                PriceCents = 1500,
            };
            db.Events.Add(ev);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            eventId = ev.Id;
        }
        await factory.CreateClient().PostAsJsonAsync($"/api/v1/events/{eventId}/bookings", new { email = "guest@example.com", paymentMethodId = "pm_card_visa" }, TestContext.Current.CancellationToken);
        return eventId;
    }

    [Fact]
    public async Task Orders_WithEventId_ListsOnlyThatEventsOrders()
    {
        var wanted = await BookNewEventAsync("Filter Wanted Event");
        await BookNewEventAsync("Filter Other Event");

        var body = await (await SignedInAsync("admin@example.com")).GetStringAsync($"/api/v1/admin/orders?eventId={wanted}", TestContext.Current.CancellationToken);

        var orders = JsonDocument.Parse(body).RootElement.GetProperty("data").EnumerateArray().ToList();
        Assert.Single(orders);
        Assert.Equal(wanted, orders[0].GetProperty("eventId").GetInt32());
    }

    [Fact]
    public async Task Orders_WithMalformedCursor_Returns400()
    {
        var response = await (await SignedInAsync("admin@example.com")).GetAsync("/api/v1/admin/orders?after=garbage", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Summary_Anonymously_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/admin/summary", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Summary_AsRegularUser_Returns403()
    {
        var response = await (await SignedInAsync("john@example.com")).GetAsync("/api/v1/admin/summary", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Summary_AsAdmin_ReportsSeatsAndRevenuePerEvent()
    {
        int eventId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ev = new Event
            {
                Title = "Summary Event",
                Description = "For summary tests.",
                StartTime = DateTime.UtcNow.AddDays(6),
                EndTime = DateTime.UtcNow.AddDays(6).AddHours(2),
                Venue = "Test Hall",
                TotalSeats = 10,
                AvailableSeats = 10,
                PriceCents = 1500,
            };
            db.Events.Add(ev);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            eventId = ev.Id;
        }
        var guest = factory.CreateClient();
        await guest.PostAsJsonAsync($"/api/v1/events/{eventId}/bookings", new { email = "a@example.com", paymentMethodId = "pm_card_visa" }, TestContext.Current.CancellationToken);
        await guest.PostAsJsonAsync($"/api/v1/events/{eventId}/bookings", new { email = "b@example.com", paymentMethodId = "pm_card_visa" }, TestContext.Current.CancellationToken);

        var body = await (await SignedInAsync("admin@example.com")).GetStringAsync("/api/v1/admin/summary", TestContext.Current.CancellationToken);

        var row = JsonDocument.Parse(body).RootElement.GetProperty("data").EnumerateArray().Single(e => e.GetProperty("eventId").GetInt32() == eventId);
        Assert.Equal((10, 2, 8, 3000), (row.GetProperty("totalSeats").GetInt32(), row.GetProperty("soldSeats").GetInt32(),
            row.GetProperty("remainingSeats").GetInt32(), row.GetProperty("revenueCents").GetInt32()));
    }
}
