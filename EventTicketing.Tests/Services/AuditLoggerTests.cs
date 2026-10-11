using System.Text.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Services;
using EventTicketing.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Services;

[Trait("Category", "Integration")]
public class AuditLoggerTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private AppDbContext CreateDb() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task Record_IsSavedByTheCallersSaveChanges()
    {
        var db = CreateDb();

        new AuditLogger(db).Record(AuditAction.EventUpdated, "Event", 42, new AuditActor(7, "admin@example.com"), new { Title = "Jazz Night" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var entry = await CreateDb().AuditLogs.SingleAsync(a => a.EntityId == 42, TestContext.Current.CancellationToken);
        Assert.Equal((AuditAction.EventUpdated, "Event", 7, "admin@example.com"), (entry.Action, entry.EntityType, entry.UserId, entry.UserEmail));
        Assert.Equal("Jazz Night", JsonDocument.Parse(entry.Details!).RootElement.GetProperty("title").GetString());
        Assert.InRange(entry.CreatedAt, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);
    }

    [Fact]
    public async Task Record_ForAnAnonymousCallerWithoutDetails_StoresNulls()
    {
        var db = CreateDb();

        new AuditLogger(db).Record(AuditAction.TicketBooked, "Order", 43, new AuditActor(null, null));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var entry = await CreateDb().AuditLogs.SingleAsync(a => a.EntityId == 43, TestContext.Current.CancellationToken);
        Assert.Null(entry.UserId);
        Assert.Null(entry.UserEmail);
        Assert.Null(entry.Details);
    }

    [Fact]
    public async Task Record_WithoutSaveChanges_StoresNothing()
    {
        new AuditLogger(CreateDb()).Record(AuditAction.EventDeleted, "Event", 44, new AuditActor(7, "admin@example.com"));

        Assert.False(await CreateDb().AuditLogs.AnyAsync(a => a.EntityId == 44, TestContext.Current.CancellationToken));
    }
}
