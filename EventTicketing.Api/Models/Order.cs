using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Models;

public class Order
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public Event Event { get; set; } = null!;

    // Null for guest bookings.
    public int? UserId { get; set; }

    public User? User { get; set; }

    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    public int PriceCents { get; set; }

    public DateTime BookedAt { get; set; }
}
