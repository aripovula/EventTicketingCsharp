using System.Text.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;

namespace EventTicketing.Api.Services;

public class AuditLogger(AppDbContext db)
{
    /// <summary>
    /// Adds an audit entry without saving it: the caller's next SaveChanges writes it together
    /// with the change it records, so either both are stored or neither is.
    /// </summary>
    public void Record(string action, string entityType, int? entityId, AuditActor actor, object? details = null) =>
        db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = actor.UserId,
            UserEmail = actor.Email,
            Details = details is null ? null : JsonSerializer.Serialize(details, JsonSerializerOptions.Web),
            CreatedAt = DateTime.UtcNow,
        });
}
