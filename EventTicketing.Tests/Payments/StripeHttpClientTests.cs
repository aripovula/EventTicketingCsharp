using System.Net;
using System.Text;
using EventTicketing.Api.Payments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Stripe;

namespace EventTicketing.Tests.Payments;

public class StripeHttpClientTests
{
    // Answers with the given statuses in order (the last one repeats) and counts the requests.
    private sealed class StubHandler(params HttpStatusCode[] statuses) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var status = statuses[Math.Min(++Calls, statuses.Length) - 1];
            var body = status switch
            {
                HttpStatusCode.OK => """{"id": "pi_123", "object": "payment_intent", "status": "succeeded"}""",
                HttpStatusCode.PaymentRequired => """{"error": {"type": "card_error", "message": "Your card was declined."}}""",
                _ => """{"error": {"type": "api_error", "message": "Stripe is having a bad day."}}""",
            };
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    // The app's Stripe HttpClient, resilience and all, in front of the stub instead of the network.
    private static StripePaymentGateway Gateway(StubHandler handler)
    {
        var services = new ServiceCollection();
        services.AddStripeHttpClient().ConfigurePrimaryHttpMessageHandler(() => handler);
        var httpClient = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient(StripeHttpClient.Name);
        return new StripePaymentGateway(
            new StripeClient("sk_test_stub", httpClient: new SystemNetHttpClient(httpClient, maxNetworkRetries: 0)),
            Options.Create(new PaymentsOptions()));
    }

    private static Task<string> ChargeAsync(StripePaymentGateway gateway) =>
        gateway.ChargeAsync(2500, "pm_card_visa", null, "key-123", TestContext.Current.CancellationToken);

    [Fact]
    public async Task Charge_IsRetriedAfterABriefFailure()
    {
        var handler = new StubHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);

        var paymentId = await ChargeAsync(Gateway(handler));

        Assert.Equal("pi_123", paymentId);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task DeclinedCharge_IsNotRetried()
    {
        var handler = new StubHandler(HttpStatusCode.PaymentRequired, HttpStatusCode.OK);

        await Assert.ThrowsAsync<PaymentDeclinedException>(() => ChargeAsync(Gateway(handler)));

        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Charge_FailsFastOnceStripeKeepsFailing()
    {
        var handler = new StubHandler(HttpStatusCode.InternalServerError);
        var gateway = Gateway(handler);

        // Each charge is tried 3 times, so the second one reaches the breaker's minimum of 5 calls and opens it.
        await Assert.ThrowsAsync<StripeException>(() => ChargeAsync(gateway));
        await Assert.ThrowsAsync<PaymentsUnavailableException>(() => ChargeAsync(gateway));
        var callsBefore = handler.Calls;

        var e = await Assert.ThrowsAsync<PaymentsUnavailableException>(() => ChargeAsync(gateway));

        Assert.Equal(StripeHttpClient.BreakDuration, e.RetryAfter);
        Assert.Equal(callsBefore, handler.Calls);
    }

}
