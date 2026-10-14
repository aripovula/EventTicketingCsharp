using System.Net;
using System.Net.Http.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Api.Payments;
using EventTicketing.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class PaymentsUnavailableIntegrationTests(PaymentsUnavailableIntegrationTests.HostWithStripeDown factory)
    : IClassFixture<PaymentsUnavailableIntegrationTests.HostWithStripeDown>
{
    public class HostWithStripeDown : IntegrationTestFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services => services.AddScoped<IPaymentGateway, UnavailablePaymentGateway>());
        }
    }

    private async Task<int> CreateEventAsync(int priceCents)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ev = new Event
        {
            Title = $"Stripe down {Guid.NewGuid():N}"[..20],
            Description = "For payments-unavailable tests.",
            StartTime = DateTime.UtcNow.AddDays(5),
            EndTime = DateTime.UtcNow.AddDays(5).AddHours(2),
            Venue = "Test Hall",
            TotalSeats = 10,
            AvailableSeats = 3,
            PriceCents = priceCents,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return ev.Id;
    }

    private async Task<int> AvailableSeatsAsync(int eventId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Events
            .Where(e => e.Id == eventId).Select(e => e.AvailableSeats).SingleAsync(TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> BookAsync(int eventId) =>
        factory.CreateClient().PostAsJsonAsync($"/api/v1/events/{eventId}/bookings",
            new { email = "guest@example.com", paymentMethodId = "pm_card_visa" }, TestContext.Current.CancellationToken);

    private static async Task AssertUnavailableAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(30), response.Headers.RetryAfter?.Delta);
        Assert.Contains("\"code\":\"payments_unavailable\"", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PaidBooking_Returns503AndReleasesTheSeat()
    {
        var eventId = await CreateEventAsync(priceCents: 2500);

        var response = await BookAsync(eventId);

        await AssertUnavailableAsync(response);
        Assert.Equal(3, await AvailableSeatsAsync(eventId));
    }

    [Fact]
    public async Task FreeBooking_StillSucceeds()
    {
        var eventId = await CreateEventAsync(priceCents: 0);

        var response = await BookAsync(eventId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SavingACard_Returns503()
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/v1/auth/register", new { name = "Card Holder", email, password = "Password1" }, TestContext.Current.CancellationToken);
        await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Password1" }, TestContext.Current.CancellationToken);

        var response = await client.PutAsJsonAsync("/api/v1/auth/me/payment-method", new { paymentMethodId = "pm_card_visa" }, TestContext.Current.CancellationToken);

        await AssertUnavailableAsync(response);
    }
}
