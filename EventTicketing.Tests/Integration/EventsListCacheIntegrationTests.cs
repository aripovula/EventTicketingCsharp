using System.Net.Http.Json;
using System.Text.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Integration;

// Its own class (and so its own database) because these tests add events behind the API's back.
[Trait("Category", "Integration")]
public class EventsListCacheIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string[]> TitlesAsync(string url)
    {
        var body = JsonDocument.Parse(await _client.GetStringAsync(url, TestContext.Current.CancellationToken)).RootElement;
        return body.GetProperty("data").EnumerateArray().Select(e => e.GetProperty("title").GetString()!).ToArray();
    }

    private async Task InsertBehindTheApiAsync(string title)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Events.Add(new Event
        {
            Title = title,
            Description = "Inserted straight into the database.",
            StartTime = DateTime.UtcNow.AddDays(3),
            EndTime = DateTime.UtcNow.AddDays(3).AddHours(2),
            Venue = "Test Hall",
            TotalSeats = 10,
            AvailableSeats = 10,
            PriceCents = 100,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task List_ServesARepeatRequestFromTheCache()
    {
        // "!" sorts before letters, so the new event would lead page one if it were read from the database.
        await TitlesAsync("/api/v1/events?sort=name&limit=3");
        await InsertBehindTheApiAsync("!Cached Away");

        Assert.DoesNotContain("!Cached Away", await TitlesAsync("/api/v1/events?sort=name&limit=3"));
    }

    [Fact]
    public async Task List_SearchesBypassTheCache()
    {
        await TitlesAsync("/api/v1/events?q=Uncached");
        await InsertBehindTheApiAsync("Uncached Search Hit");

        Assert.Contains("Uncached Search Hit", await TitlesAsync("/api/v1/events?q=Uncached"));
    }

    private async Task<HttpClient> AdminAsync()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "admin@example.com", password = "Password" }, TestContext.Current.CancellationToken);
        return client;
    }

    private static object EventRequest(string title) => new
    {
        title,
        description = "Written through the admin API.",
        startTime = DateTime.UtcNow.AddDays(4),
        endTime = DateTime.UtcNow.AddDays(4).AddHours(2),
        venue = "Test Hall",
        eventType = "Other",
        totalSeats = 10,
        availableSeats = 10,
        priceCents = 100,
    };

    private const string FirstPage = "/api/v1/events?sort=name&limit=5";

    private async Task<int> CreateAsync(HttpClient admin, string title)
    {
        var response = await admin.PostAsJsonAsync("/api/v1/events", EventRequest(title), TestContext.Current.CancellationToken);
        return (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("data").GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task CreatingAnEvent_EvictsTheCachedPages()
    {
        var admin = await AdminAsync();
        await TitlesAsync(FirstPage);

        await CreateAsync(admin, "!!Created Event");

        Assert.Contains("!!Created Event", await TitlesAsync(FirstPage));
    }

    [Fact]
    public async Task UpdatingAnEvent_EvictsTheCachedPages()
    {
        var admin = await AdminAsync();
        var id = await CreateAsync(admin, "!!!Before Edit");
        await TitlesAsync(FirstPage);

        await admin.PutAsJsonAsync($"/api/v1/events/{id}", EventRequest("!!!After Edit"), TestContext.Current.CancellationToken);

        Assert.Contains("!!!After Edit", await TitlesAsync(FirstPage));
    }

    [Fact]
    public async Task DeletingAnEvent_EvictsTheCachedPages()
    {
        var admin = await AdminAsync();
        var id = await CreateAsync(admin, "!!!!Doomed Event");
        Assert.Contains("!!!!Doomed Event", await TitlesAsync(FirstPage));

        await admin.DeleteAsync($"/api/v1/events/{id}", TestContext.Current.CancellationToken);

        Assert.DoesNotContain("!!!!Doomed Event", await TitlesAsync(FirstPage));
    }

    [Fact]
    public async Task BookingATicket_EvictsTheCachedPages()
    {
        var admin = await AdminAsync();
        var id = await CreateAsync(admin, "!!!!!Booked Event");
        Assert.False(await HasOrdersAsync(id));

        await _client.PostAsJsonAsync($"/api/v1/events/{id}/bookings",
            new { email = "guest@example.com", paymentMethodId = "pm_card_visa" }, TestContext.Current.CancellationToken);

        Assert.True(await HasOrdersAsync(id));
    }

    private async Task<bool> HasOrdersAsync(int id)
    {
        var body = JsonDocument.Parse(await _client.GetStringAsync(FirstPage, TestContext.Current.CancellationToken)).RootElement;
        return body.GetProperty("data").EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == id).GetProperty("hasOrders").GetBoolean();
    }
}
