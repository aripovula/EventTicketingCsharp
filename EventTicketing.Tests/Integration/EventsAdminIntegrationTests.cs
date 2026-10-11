using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

    private async Task<AuditLog> AuditEntryAsync(string action, int eventId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().AuditLogs
            .SingleAsync(a => a.Action == action && a.EntityType == "Event" && a.EntityId == eventId, TestContext.Current.CancellationToken);
    }

    private static void AssertRecordedForAdmin(AuditLog entry, string title)
    {
        Assert.NotNull(entry.UserId);
        Assert.Equal("admin@example.com", entry.UserEmail);
        Assert.Equal(title, JsonDocument.Parse(entry.Details!).RootElement.GetProperty("title").GetString());
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

    [Fact]
    public async Task Create_AsAdmin_Returns201WithLocationOfTheNewEvent()
    {
        var admin = await SignedInAsync("admin@example.com");

        var response = await admin.PostAsJsonAsync("/api/v1/events", NewEvent("Brand New Gig"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.GetProperty("data");
        Assert.Equal("Brand New Gig", created.GetProperty("title").GetString());
        var fetched = await admin.GetStringAsync(response.Headers.Location, TestContext.Current.CancellationToken);
        Assert.Contains("Brand New Gig", fetched);
    }

    [Fact]
    public async Task Update_Anonymously_Returns401()
    {
        var response = await factory.CreateClient().PutAsJsonAsync("/api/v1/events/1", NewEvent(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_AsRegularUser_Returns403()
    {
        var client = await SignedInAsync("jane@example.com");

        var response = await client.PutAsJsonAsync("/api/v1/events/1", NewEvent(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<int> CreateAsAdminAsync(HttpClient admin, string title)
    {
        var response = await admin.PostAsJsonAsync("/api/v1/events", NewEvent(title), TestContext.Current.CancellationToken);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.GetProperty("data").GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Create_AsAdmin_RecordsWhoCreatedIt()
    {
        var id = await CreateAsAdminAsync(await SignedInAsync("admin@example.com"), "Audited Gig");

        AssertRecordedForAdmin(await AuditEntryAsync(AuditAction.EventCreated, id), "Audited Gig");
    }

    [Fact]
    public async Task Update_AsAdmin_Returns204AndSavesTheChanges()
    {
        var admin = await SignedInAsync("admin@example.com");
        var id = await CreateAsAdminAsync(admin, "Before Edit");

        var response = await admin.PutAsJsonAsync($"/api/v1/events/{id}", NewEvent("After Edit"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var fetched = await admin.GetStringAsync($"/api/v1/events/{id}", TestContext.Current.CancellationToken);
        Assert.Contains("After Edit", fetched);
    }

    [Fact]
    public async Task Update_AsAdmin_RecordsWhoUpdatedIt()
    {
        var admin = await SignedInAsync("admin@example.com");
        var id = await CreateAsAdminAsync(admin, "Before Audit");

        await admin.PutAsJsonAsync($"/api/v1/events/{id}", NewEvent("After Audit"), TestContext.Current.CancellationToken);

        AssertRecordedForAdmin(await AuditEntryAsync(AuditAction.EventUpdated, id), "After Audit");
    }

    [Fact]
    public async Task Update_UnknownEvent_Returns404()
    {
        var admin = await SignedInAsync("admin@example.com");

        var response = await admin.PutAsJsonAsync("/api/v1/events/999999", NewEvent(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Anonymously_Returns401()
    {
        var response = await factory.CreateClient().DeleteAsync("/api/v1/events/1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AsRegularUser_Returns403()
    {
        var client = await SignedInAsync("alex@example.com");

        var response = await client.DeleteAsync("/api/v1/events/1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AsAdmin_Returns204AndRemovesTheEvent()
    {
        var admin = await SignedInAsync("admin@example.com");
        var id = await CreateAsAdminAsync(admin, "Doomed Event");

        var response = await admin.DeleteAsync($"/api/v1/events/{id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var fetched = await admin.GetAsync($"/api/v1/events/{id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, fetched.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownEvent_Returns404()
    {
        var admin = await SignedInAsync("admin@example.com");

        var response = await admin.DeleteAsync("/api/v1/events/999999", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
