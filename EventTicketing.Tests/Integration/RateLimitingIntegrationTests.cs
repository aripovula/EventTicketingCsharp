using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class RateLimitingIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private HttpClient ClientWith(string setting, int limit) =>
        factory.WithWebHostBuilder(builder => builder.UseSetting(setting, limit.ToString())).CreateClient();

    [Fact]
    public async Task RequestsOverTheGlobalLimit_Return429()
    {
        var client = ClientWith("RateLimiting:GlobalPermitLimit", 3);
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/events/1", TestContext.Current.CancellationToken)).StatusCode);

        var response = await client.GetAsync("/api/v1/events/1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task ARejectedRequest_GetsTheErrorEnvelopeAndRetryAfter()
    {
        var client = ClientWith("RateLimiting:GlobalPermitLimit", 1);
        await client.GetAsync("/api/v1/events/1", TestContext.Current.CancellationToken);

        var response = await client.GetAsync("/api/v1/events/1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Contains("\"code\":\"rate_limited\"", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.InRange(int.Parse(response.Headers.GetValues("Retry-After").Single()), 1, 60);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    public async Task HealthChecks_AreNeverRateLimited(string url)
    {
        var client = ClientWith("RateLimiting:GlobalPermitLimit", 1);

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(url, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task BookingsOverTheBookingLimit_Return429WhileOtherRequestsStillWork()
    {
        var client = ClientWith("RateLimiting:BookingPermitLimit", 2);
        // Event 1 is seeded with plenty of seats; two bookings use up the allowance.
        for (var i = 0; i < 2; i++)
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/events/1/bookings", new { email = "guest@example.com", paymentMethodId = "pm_card_visa" }, TestContext.Current.CancellationToken)).StatusCode);

        var booking = await client.PostAsJsonAsync("/api/v1/events/1/bookings", new { email = "guest@example.com", paymentMethodId = "pm_card_visa" }, TestContext.Current.CancellationToken);
        var browsing = await client.GetAsync("/api/v1/events/1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, booking.StatusCode);
        Assert.Equal(HttpStatusCode.OK, browsing.StatusCode);
    }
}
