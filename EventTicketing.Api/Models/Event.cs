using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Models;

public class Event : ITimestamped
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    [Required]
    [MaxLength(200)]
    public string Venue { get; set; } = string.Empty;

    [MaxLength(50)]
    public string EventType { get; set; } = "Other";

    [Range(1, 10_000)]
    public int TotalSeats { get; set; }

    [Range(0, 10_000)]
    public int AvailableSeats { get; set; }

    [Range(0, 10_000_000)]
    public int PriceCents { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
