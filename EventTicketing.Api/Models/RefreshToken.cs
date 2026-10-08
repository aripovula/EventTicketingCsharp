using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Models;

public class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    // SHA-256 of the raw token; the raw value only ever lives in the client's cookie.
    [Required]
    [MaxLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    // Every token issued by rotating the same login shares a family, so reuse
    // of a revoked token can revoke the whole chain.
    public Guid FamilyId { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public int? ReplacedById { get; set; }

    public DateTime CreatedAt { get; set; }
}
