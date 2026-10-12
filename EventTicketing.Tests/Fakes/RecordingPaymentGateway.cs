using EventTicketing.Api.Payments;

namespace EventTicketing.Tests.Fakes;

public class RecordingPaymentGateway : IPaymentGateway
{
    public List<(int AmountCents, string PaymentMethodId, string? CustomerId, string IdempotencyKey)> Charges { get; } = [];

    public List<string> Refunds { get; } = [];

    // When set, every charge is declined with this message.
    public string? DeclineWith { get; set; }

    // When set, every charge throws it, as if Stripe were unreachable.
    public Exception? FailWith { get; set; }

    // When set, every refund throws it.
    public Exception? RefundFailsWith { get; set; }

    public Task<string> ChargeAsync(int amountCents, string paymentMethodId, string? customerId, string idempotencyKey, CancellationToken cancellationToken)
    {
        if (FailWith is not null)
            throw FailWith;
        if (DeclineWith is not null)
            throw new PaymentDeclinedException(DeclineWith);
        Charges.Add((amountCents, paymentMethodId, customerId, idempotencyKey));
        return Task.FromResult($"pi_test_{Charges.Count}");
    }

    public Task RefundAsync(string paymentId, CancellationToken cancellationToken)
    {
        if (RefundFailsWith is not null)
            throw RefundFailsWith;
        Refunds.Add(paymentId);
        return Task.CompletedTask;
    }

    public Task<SavedCard> SaveCardAsync(string? customerId, string email, string paymentMethodId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
