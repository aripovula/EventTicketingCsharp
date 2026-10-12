using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Models;

/// <summary>
/// A card saved to a user's Stripe customer. Stripe holds the card; we keep only its ids and what
/// we show the user ("Visa ending in 4242, expires 12/2030").
/// </summary>
public class PaymentMethod
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string StripeCustomerId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string StripePaymentMethodId { get; set; } = string.Empty;

    // Stripe's lowercase brand name, e.g. "visa" or "mastercard".
    [Required]
    [MaxLength(20)]
    public string Brand { get; set; } = string.Empty;

    [Required]
    [StringLength(4, MinimumLength = 4)]
    public string Last4 { get; set; } = string.Empty;

    public int ExpMonth { get; set; }

    public int ExpYear { get; set; }

    // The card bookings use; saving a new card keeps the old row but stops it being the default.
    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }
}
