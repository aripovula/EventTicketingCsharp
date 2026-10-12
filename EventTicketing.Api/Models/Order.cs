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

    // The Stripe PaymentIntent that paid for the ticket; null for orders placed before payments existed.
    [MaxLength(255)]
    public string? StripePaymentIntentId { get; set; }

    // SHA-256 of the lookup token a guest receives at booking time; null for signed-in bookings.
    [MaxLength(64)]
    public string? GuestAccessTokenHash { get; set; }
}
