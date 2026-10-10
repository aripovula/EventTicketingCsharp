using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class IdempotencyIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private async Task<int> CreateEventAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ev = new Event
        {
            Title = "Idempotency Event",
            Description = "For idempotency tests.",
            StartTime = DateTime.UtcNow.AddDays(5),
            EndTime = DateTime.UtcNow.AddDays(5).AddHours(2),
            Venue = "Test Hall",
            TotalSeats = 10,
            AvailableSeats = 10,
            PriceCents = 2500,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return ev.Id;
    }

    private async Task<(HttpStatusCode Status, JsonElement Data)> BookAsync(int eventId, string? key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/events/{eventId}/bookings")
        {
            Content = JsonContent.Create(new { email = "guest@example.com" }),
        };
        if (key is not null)
            request.Headers.Add("Idempotency-Key", key);
        var response = await factory.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
        return (response.StatusCode, body.GetProperty("data").Clone());
    }

    private async Task<int> SeatsLeftAsync(int eventId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.Events.SingleAsync(e => e.Id == eventId, TestContext.Current.CancellationToken)).AvailableSeats;
    }

    [Fact]
    public async Task ARetryWithTheSameKey_ReplaysTheFirstBookingInsteadOfBookingAgain()
    {
        var eventId = await CreateEventAsync();
        var key = Guid.NewGuid().ToString();

        var first = await BookAsync(eventId, key);
        var retry = await BookAsync(eventId, key);

        Assert.Equal(HttpStatusCode.Created, retry.Status);
        Assert.Equal(first.Data.GetProperty("id").GetInt32(), retry.Data.GetProperty("id").GetInt32());
        // A guest's retry still gets the link to their order.
        Assert.Equal(first.Data.GetProperty("guestAccessToken").GetString(), retry.Data.GetProperty("guestAccessToken").GetString());
        Assert.Equal(9, await SeatsLeftAsync(eventId));
    }

    [Fact]
    public async Task DifferentKeys_BookSeparately()
    {
        var eventId = await CreateEventAsync();

        var first = await BookAsync(eventId, Guid.NewGuid().ToString());
        var second = await BookAsync(eventId, Guid.NewGuid().ToString());

        Assert.NotEqual(first.Data.GetProperty("id").GetInt32(), second.Data.GetProperty("id").GetInt32());
        Assert.Equal(8, await SeatsLeftAsync(eventId));
    }

    [Fact]
    public async Task RequestsWithoutAKey_BookEveryTime()
    {
        var eventId = await CreateEventAsync();

        await BookAsync(eventId, null);
        await BookAsync(eventId, null);

        Assert.Equal(8, await SeatsLeftAsync(eventId));
    }
}
