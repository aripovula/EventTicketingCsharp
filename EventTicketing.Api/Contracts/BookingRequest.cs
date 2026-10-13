using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Contracts;

public record BookingRequest
{
    /// <summary>Where the ticket goes; required for guests and signed-in users alike.</summary>
    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// The card to charge: a Stripe PaymentMethod id created in the browser by Stripe Elements
    /// (in test mode, e.g. <c>pm_card_visa</c>). Card details never reach this API.
    /// Required for paid events; free events need none.
    /// </summary>
    [MaxLength(255)]
    public string? PaymentMethodId { get; init; }

    /// <summary>Pay with the signed-in user's saved card (see <c>PUT /api/v1/auth/me/payment-method</c>) instead of <see cref="PaymentMethodId"/>.</summary>
    public bool UseSavedPaymentMethod { get; init; }
}
