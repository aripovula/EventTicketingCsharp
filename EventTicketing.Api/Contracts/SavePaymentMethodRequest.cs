using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Contracts;

public record SavePaymentMethodRequest
{
    /// <summary>
    /// The card to save: a Stripe PaymentMethod id created in the browser by Stripe Elements
    /// (in test mode, e.g. <c>pm_card_visa</c>). Card details never reach this API.
    /// </summary>
    [Required, MaxLength(255)]
    public string PaymentMethodId { get; init; } = string.Empty;
}
