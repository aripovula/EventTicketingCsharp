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
            $"/api/v1/events/{eventId}/bookings", new { email = "jane@example.com" }, TestContext.Current.CancellationToken);
        var orderId = JsonDocument.Parse(await booked.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var body = await (await SignedInAsync("admin@example.com")).GetStringAsync("/api/v1/admin/orders", TestContext.Current.CancellationToken);

        Assert.Contains(JsonDocument.Parse(body).RootElement.GetProperty("data").EnumerateArray(), o => o.GetProperty("id").GetInt32() == orderId);
    }

    [Fact]
    public async Task Orders_WithMalformedCursor_Returns400()
    {
        var response = await (await SignedInAsync("admin@example.com")).GetAsync("/api/v1/admin/orders?after=garbage", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Summary_AsRegularUser_Returns403()
    {
        var response = await (await SignedInAsync("john@example.com")).GetAsync("/api/v1/admin/summary", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
