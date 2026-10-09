using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class MyOrdersIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task MyOrders_WithoutSigningIn_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/auth/me/orders", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<int> CreateEventAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ev = new Event
        {
            Title = $"My Orders {Guid.NewGuid():N}"[..17],
            Description = "For my-orders tests.",
            StartTime = DateTime.UtcNow.AddDays(5),
            EndTime = DateTime.UtcNow.AddDays(5).AddHours(2),
            Venue = "Test Hall",
            TotalSeats = 50,
            AvailableSeats = 50,
            PriceCents = 2500,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return ev.Id;
    }

    private async Task<HttpClient> SignedInAsync(string email)
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Password" }, TestContext.Current.CancellationToken);
        return client;
    }

    private static async Task<int> BookAsync(HttpClient client, int eventId, string email)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/events/{eventId}/bookings", new { email }, TestContext.Current.CancellationToken);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.GetProperty("data").GetProperty("id").GetInt32();
    }

    private static async Task<int[]> OrderIdsAsync(HttpClient client, string url)
    {
        var body = JsonDocument.Parse(await client.GetStringAsync(url, TestContext.Current.CancellationToken)).RootElement;
        return body.GetProperty("data").EnumerateArray().Select(o => o.GetProperty("id").GetInt32()).ToArray();
    }

    [Fact]
    public async Task MyOrders_ListsOnlyTheSignedInUsersOrdersNewestFirst()
    {
        var eventId = await CreateEventAsync();
        var jane = await SignedInAsync("jane@example.com");
        var first = await BookAsync(jane, eventId, "jane@example.com");
        var second = await BookAsync(jane, eventId, "jane@example.com");
        await BookAsync(await SignedInAsync("alex@example.com"), eventId, "alex@example.com");

        var ids = await OrderIdsAsync(jane, "/api/v1/auth/me/orders");

        Assert.Equal([second, first], ids);
    }
}
