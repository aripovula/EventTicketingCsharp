namespace EventTicketing.Api.Payments;

/// <summary>
/// Approves every card except Stripe's decline test id, without calling Stripe: for E2E runs
/// without Stripe keys (Payments:Provider = Fake). Never allowed in Production.
/// </summary>
public class FakePaymentGateway : IPaymentGateway
{
    // The same test ids Stripe accepts, so the frontend's test-card choice works against either gateway.
    public const string ApprovedPaymentMethod = "pm_card_visa";

    public const string DeclinedPaymentMethod = "pm_card_chargeDeclined";

    public Task<string> ChargeAsync(int amountCents, string paymentMethodId, string? customerId, string idempotencyKey, CancellationToken cancellationToken)
    {
        if (paymentMethodId == DeclinedPaymentMethod)
            throw new PaymentDeclinedException("Your card was declined.");

        return Task.FromResult($"pi_fake_{Guid.NewGuid():N}");
    }

    public Task RefundAsync(string paymentId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<SavedCard> SaveCardAsync(string? customerId, string email, string paymentMethodId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
