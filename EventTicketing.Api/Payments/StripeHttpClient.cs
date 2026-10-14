using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace EventTicketing.Api.Payments;

/// <summary>
/// The HttpClient the Stripe SDK sends through: retries brief failures, and when Stripe keeps failing,
/// a circuit breaker stops calling it for a while so bookings fail fast instead of waiting on timeouts.
/// </summary>
public static class StripeHttpClient
{
    public const string Name = "stripe";

    /// <summary>How long the breaker stays open before letting a trial request through.</summary>
    public static readonly TimeSpan BreakDuration = TimeSpan.FromSeconds(30);

    public static IHttpClientBuilder AddStripeHttpClient(this IServiceCollection services)
    {
        var client = services.AddHttpClient(Name);
        client.AddResilienceHandler(Name, pipeline => pipeline
            // Outermost: the whole call, retries included, gives up after 30 seconds.
            .AddTimeout(TimeSpan.FromSeconds(30))
            .AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(500),
                ShouldHandle = args => ValueTask.FromResult(
                    HttpClientResiliencePredicates.IsTransient(args.Outcome) && IsSafeToRepeat(args.Context.GetRequestMessage())),
            })
            // Opens when half of at least 5 calls in 30 seconds fail; each retry counts as a call.
            .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = BreakDuration,
            })
            // Innermost: one attempt gets 10 seconds.
            .AddTimeout(TimeSpan.FromSeconds(10)));
        return client;
    }

    // A POST can be repeated only with an idempotency key: Stripe then returns the first result
    // instead of charging the card again.
    private static bool IsSafeToRepeat(HttpRequestMessage? request) =>
        request is not null && (request.Method == HttpMethod.Get || request.Method == HttpMethod.Delete
            || request.Headers.Contains("Idempotency-Key"));
}
