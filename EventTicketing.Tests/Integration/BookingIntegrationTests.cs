using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Api.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class BookingIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Book_WithMalformedEmail_Returns400ForEmail()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/events/1/bookings", new { email = "not-an-email", paymentMethodId = "pm_card_visa" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"field\":\"email\"", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Book_PaidEventWithoutAPaymentMethod_Returns400ForPaymentMethodId()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/events/1/bookings", new { email = "guest@example.com" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"field\":\"paymentMethodId\"", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private async Task<int> CreateEventAsync(int availableSeats, int priceCents = 2500)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ev = new Event
        {
            Title = $"Booking API {Guid.NewGuid():N}"[..20],
            Description = "For booking API tests.",
            StartTime = DateTime.UtcNow.AddDays(5),
            EndTime = DateTime.UtcNow.AddDays(5).AddHours(2),
            Venue = "Test Hall",
            TotalSeats = 10,
            AvailableSeats = availableSeats,
            PriceCents = priceCents,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return ev.Id;
    }

    private Task<HttpResponseMessage> BookAsync(int eventId, HttpClient? client = null) =>
        (client ?? _client).PostAsJsonAsync($"/api/v1/events/{eventId}/bookings", new { email = "guest@example.com", paymentMethodId = "pm_card_visa" }, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Book_WithSeatsLeft_Returns201WithTheOrder()
    {
        var eventId = await CreateEventAsync(availableSeats: 3);

        var response = await BookAsync(eventId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.GetProperty("data");
        Assert.Equal(eventId, order.GetProperty("eventId").GetInt32());
        Assert.Equal(2500, order.GetProperty("priceCents").GetInt32());
    }

    [Fact]
    public async Task Book_SoldOutEvent_Returns409SoldOut()
    {
        var eventId = await CreateEventAsync(availableSeats: 0);

        var response = await BookAsync(eventId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("\"code\":\"sold_out\"", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Book_WithADeclinedCard_Returns402AndKeepsTheSeat()
    {
        var eventId = await CreateEventAsync(availableSeats: 3);

        var response = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/bookings",
            new { email = "guest@example.com", paymentMethodId = FakePaymentGateway.DeclinedPaymentMethod }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
        Assert.Contains("\"code\":\"payment_declined\"", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        using var scope = factory.Services.CreateScope();
        var ev = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Events.SingleAsync(e => e.Id == eventId, TestContext.Current.CancellationToken);
        Assert.Equal(3, ev.AvailableSeats);
    }

    [Fact]
    public async Task Book_AFreeEventWithoutAPaymentMethod_Returns201()
    {
        var eventId = await CreateEventAsync(availableSeats: 3, priceCents: 0);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/events/{eventId}/bookings", new { email = "guest@example.com" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Book_UnknownEvent_Returns404()
    {
        var response = await BookAsync(999_999);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Book_TenBuyersRacingForTheLastSeat_ExactlyOneSucceeds()
    {
        var eventId = await CreateEventAsync(availableSeats: 1);

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => BookAsync(eventId, factory.CreateClient())));

        var statuses = responses.Select(r => r.StatusCode).ToList();
        Assert.Equal(1, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(9, statuses.Count(s => s == HttpStatusCode.Conflict));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, (await db.Events.SingleAsync(e => e.Id == eventId, TestContext.Current.CancellationToken)).AvailableSeats);
        Assert.Equal(1, await db.Orders.CountAsync(o => o.EventId == eventId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Book_PointsLocationAtTheNewOrder()
    {
        var eventId = await CreateEventAsync(availableSeats: 3);

        var response = await BookAsync(eventId);

        var orderId = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.GetProperty("data").GetProperty("id").GetInt32();
        Assert.Equal($"/api/v1/orders/{orderId}", response.Headers.Location?.AbsolutePath);
    }
}
