using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Models;

/// <summary>
/// One entry in the audit trail: who did what to which entity. Deliberately no foreign keys,
/// so the trail outlives the events and users it mentions.
/// </summary>
public class AuditLog
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    // Null when there is no entity, e.g. a failed sign-in with an unknown email.
    public int? EntityId { get; set; }

    // Null for guests and unknown emails.
    public int? UserId { get; set; }

    [MaxLength(200)]
    public string? UserEmail { get; set; }

    // Extra context as JSON (e.g. the event title), stored as jsonb.
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }
}
