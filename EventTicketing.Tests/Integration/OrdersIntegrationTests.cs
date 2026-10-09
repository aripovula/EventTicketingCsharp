using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class OrdersIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Get_WithNonNumericId_Returns404()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/orders/abc", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<JsonElement> GuestBookingAsync()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Events.Add(new Event
            {
                Title = "Orders API Event",
                Description = "For order API tests.",
                StartTime = DateTime.UtcNow.AddDays(5),
                EndTime = DateTime.UtcNow.AddDays(5).AddHours(2),
                Venue = "Test Hall",
                TotalSeats = 10,
                AvailableSeats = 10,
                PriceCents = 2500,
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var list = JsonDocument.Parse(await factory.CreateClient().GetStringAsync("/api/v1/events?q=Orders%20API%20Event", TestContext.Current.CancellationToken));
        var eventId = list.RootElement.GetProperty("data")[0].GetProperty("id").GetInt32();
        var booked = await factory.CreateClient().PostAsJsonAsync($"/api/v1/events/{eventId}/bookings", new { email = "guest@example.com" }, TestContext.Current.CancellationToken);
        return JsonDocument.Parse(await booked.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.GetProperty("data").Clone();
    }

    [Fact]
    public async Task Get_GuestOrderWithItsToken_Returns200()
    {
        var order = await GuestBookingAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/orders/{order.GetProperty("id").GetInt32()}");
        request.Headers.Add("X-Order-Token", order.GetProperty("guestAccessToken").GetString());

        var response = await factory.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_GuestOrderWithoutToken_Returns404NotFound()
    {
        var order = await GuestBookingAsync();

        var response = await factory.CreateClient().GetAsync($"/api/v1/orders/{order.GetProperty("id").GetInt32()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("\"code\":\"not_found\"", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Get_GuestOrderAsAdmin_Returns200()
    {
        var order = await GuestBookingAsync();
        var admin = factory.CreateClient();
        await admin.PostAsJsonAsync("/api/v1/auth/login", new { email = "admin@example.com", password = "Password" }, TestContext.Current.CancellationToken);

        var response = await admin.GetAsync($"/api/v1/orders/{order.GetProperty("id").GetInt32()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
