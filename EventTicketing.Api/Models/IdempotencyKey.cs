using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Models;

/// <summary>A response remembered for 24 hours so a retried request with the same Idempotency-Key replays it.</summary>
public class IdempotencyKey
{
    public int Id { get; set; }

    [Required]
    [MaxLength(128)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string RequestPath { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    // The full enveloped JSON body. Stored as jsonb, so a replay may differ from the original in spacing and key order.
    [Required]
    public string ResponseBody { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}
