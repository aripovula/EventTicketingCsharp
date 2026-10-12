namespace EventTicketing.Api.Payments;

public interface IPaymentGateway
{
    /// <summary>Charges the card now and returns the payment's id (a Stripe PaymentIntent id).</summary>
    /// <param name="amountCents">The amount in cents.</param>
    /// <param name="paymentMethodId">The card, tokenized in the browser by Stripe Elements.</param>
    /// <param name="customerId">The Stripe customer a saved card belongs to; null for a card used once.</param>
    /// <param name="idempotencyKey">Stripe ignores a repeat call with the same key for 24 hours, so a retry can't charge twice.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="PaymentDeclinedException">The card was refused.</exception>
    Task<string> ChargeAsync(int amountCents, string paymentMethodId, string? customerId, string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>Refunds a charge in full.</summary>
    Task RefundAsync(string paymentId, CancellationToken cancellationToken);

    /// <summary>Attaches the card to a Stripe customer so it can be charged again later.</summary>
    /// <param name="customerId">The user's Stripe customer, or null to create one for <paramref name="email"/>.</param>
    /// <exception cref="PaymentDeclinedException">The card was refused.</exception>
    Task<SavedCard> SaveCardAsync(string? customerId, string email, string paymentMethodId, CancellationToken cancellationToken);
}
