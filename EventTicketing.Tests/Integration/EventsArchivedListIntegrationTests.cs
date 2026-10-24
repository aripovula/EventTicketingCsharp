using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class EventsArchivedListIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<int> AddEventAsync(string title, DateTime? archivedAt)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ev = new Event
        {
            Title = title,
            Description = "For the archive tests.",
            StartTime = DateTime.UtcNow.AddDays(5),
            EndTime = DateTime.UtcNow.AddDays(5).AddHours(2),
            Venue = "Back Room",
            TotalSeats = 10,
            AvailableSeats = 10,
            ArchivedAt = archivedAt,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return ev.Id;
    }

    private async Task<List<string>> TitlesAsync(string query)
    {
        var body = JsonDocument.Parse(await _client.GetStringAsync($"/api/v1/events?{query}", TestContext.Current.CancellationToken));
        return body.RootElement.GetProperty("data").EnumerateArray().Select(e => e.GetProperty("title").GetString()!).ToList();
    }

    [Fact]
    public async Task TheList_LeavesOutArchivedEvents()
    {
        await AddEventAsync("Zzz Archived Gig", DateTime.UtcNow);
        await AddEventAsync("Zzz Current Gig", null);

        var titles = await TitlesAsync("sort=date&limit=50");

        Assert.Contains("Zzz Current Gig", titles);
        Assert.DoesNotContain("Zzz Archived Gig", titles);
    }

    [Fact]
    public async Task ASearch_LeavesOutArchivedEvents()
    {
        await AddEventAsync("Archived Search Gig", DateTime.UtcNow);

        Assert.Empty(await TitlesAsync("q=Archived%20Search"));
    }

    [Fact]
    public async Task AnArchivedEvent_CanStillBeOpenedByItsId()
    {
        var id = await AddEventAsync("Archived But Linked", DateTime.UtcNow);

        var body = JsonDocument.Parse(await _client.GetStringAsync($"/api/v1/events/{id}", TestContext.Current.CancellationToken));

        Assert.Equal("Archived But Linked", body.RootElement.GetProperty("data").GetProperty("title").GetString());
    }

    [Fact]
    public async Task AnAdmin_CanAskForArchivedEventsToo()
    {
        await AddEventAsync("Archived For Admins", DateTime.UtcNow);
        var admin = factory.CreateClient();
        await admin.PostAsJsonAsync("/api/v1/auth/login", new { email = "admin@example.com", password = "Password" }, TestContext.Current.CancellationToken);

        var body = JsonDocument.Parse(await admin.GetStringAsync("/api/v1/events?q=Archived%20For%20Admins&includeArchived=true", TestContext.Current.CancellationToken));

        var ev = Assert.Single(body.RootElement.GetProperty("data").EnumerateArray());
        Assert.NotEqual(JsonValueKind.Null, ev.GetProperty("archivedAt").ValueKind);
    }

    [Fact]
    public async Task AnAdmin_GetsArchivedEventsOnUnsearchedPagesToo()
    {
        await AddEventAsync("!Archived Unsearched", DateTime.UtcNow);
        var admin = factory.CreateClient();
        await admin.PostAsJsonAsync("/api/v1/auth/login", new { email = "admin@example.com", password = "Password" }, TestContext.Current.CancellationToken);

        var body = JsonDocument.Parse(await admin.GetStringAsync("/api/v1/events?sort=name&limit=50&includeArchived=true", TestContext.Current.CancellationToken));

        Assert.Contains(body.RootElement.GetProperty("data").EnumerateArray(), e => e.GetProperty("title").GetString() == "!Archived Unsearched");
    }

    [Fact]
    public async Task Others_CannotAskForArchivedEvents()
    {
        var response = await _client.GetAsync("/api/v1/events?includeArchived=true", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
