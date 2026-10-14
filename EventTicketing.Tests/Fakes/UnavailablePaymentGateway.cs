using EventTicketing.Api.Payments;

namespace EventTicketing.Tests.Fakes;

/// <summary>Behaves as the Stripe gateway does while its circuit breaker is open.</summary>
public class UnavailablePaymentGateway : IPaymentGateway
{
    public static readonly TimeSpan RetryAfter = TimeSpan.FromSeconds(30);

    public Task<string> ChargeAsync(int amountCents, string paymentMethodId, string? customerId, string idempotencyKey, CancellationToken cancellationToken) =>
        throw new PaymentsUnavailableException(RetryAfter);

    public Task RefundAsync(string paymentId, CancellationToken cancellationToken) =>
        throw new PaymentsUnavailableException(RetryAfter);

    public Task<SavedCard> SaveCardAsync(string? customerId, string email, string paymentMethodId, CancellationToken cancellationToken) =>
        throw new PaymentsUnavailableException(RetryAfter);
}
