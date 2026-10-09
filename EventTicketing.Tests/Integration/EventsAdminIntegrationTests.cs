using System.Net;
using System.Net.Http.Json;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class EventsAdminIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private static object NewEvent(string title = "Admin Created Event") => new
    {
        title,
        description = "Created by an admin.",
        startTime = DateTime.UtcNow.AddDays(10),
        endTime = DateTime.UtcNow.AddDays(10).AddHours(2),
        venue = "Main Hall",
        eventType = "Music",
        totalSeats = 100,
        availableSeats = 100,
        priceCents = 1500,
        imageUrl = (string?)null,
    };

    private async Task<HttpClient> SignedInAsync(string email)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email, password = "Password" }, TestContext.Current.CancellationToken);
        login.EnsureSuccessStatusCode();
        return client;
    }

    [Fact]
    public async Task Create_Anonymously_Returns401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/events", NewEvent(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsRegularUser_Returns403()
    {
        var client = await SignedInAsync("john@example.com");

        var response = await client.PostAsJsonAsync("/api/v1/events", NewEvent(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidEvent_Returns400()
    {
        var response = await (await SignedInAsync("admin@example.com")).PostAsJsonAsync(
            "/api/v1/events", new { title = "" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
