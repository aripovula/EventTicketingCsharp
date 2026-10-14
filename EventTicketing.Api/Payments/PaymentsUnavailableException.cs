namespace EventTicketing.Api.Payments;

/// <summary>Stripe has been failing, so we stopped calling it for a while; the buyer can try again after <see cref="RetryAfter"/>.</summary>
public class PaymentsUnavailableException(TimeSpan retryAfter, Exception? innerException = null)
    : Exception("Payments are temporarily unavailable.", innerException)
{
    public TimeSpan RetryAfter { get; } = retryAfter;
}
