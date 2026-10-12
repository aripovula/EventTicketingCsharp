using System.Net;
using System.Text;
using EventTicketing.Api.Payments;
using Microsoft.Extensions.Options;
using Stripe;

namespace EventTicketing.Tests.Payments;

public class StripePaymentGatewayTests
{
    // Answers every Stripe API call with one canned response and remembers the request.
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private static StripePaymentGateway Gateway(StubHandler handler) =>
        new(new StripeClient("sk_test_stub", httpClient: new SystemNetHttpClient(new HttpClient(handler))),
            Options.Create(new PaymentsOptions()));

    private static Task<string> ChargeAsync(StubHandler handler) =>
        Gateway(handler).ChargeAsync(2500, "pm_card_visa", null, "key-123", TestContext.Current.CancellationToken);

    [Fact]
    public async Task Charge_CreatesAndConfirmsAPaymentIntentWithTheIdempotencyKey()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"id": "pi_123", "object": "payment_intent", "status": "succeeded"}""");

        var paymentId = await ChargeAsync(handler);

        Assert.Equal("pi_123", paymentId);
        Assert.Equal("/v1/payment_intents", handler.Request!.RequestUri!.AbsolutePath);
        Assert.Equal("key-123", handler.Request.Headers.GetValues("Idempotency-Key").Single());
        var form = WebUtility.UrlDecode(handler.RequestBody!);
        Assert.Contains("amount=2500", form);
        Assert.Contains("currency=usd", form);
        Assert.Contains("payment_method=pm_card_visa", form);
        Assert.Contains("confirm=True", form, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Charge_WhenTheCardIsDeclined_ThrowsWithStripesMessage()
    {
        var handler = new StubHandler(HttpStatusCode.PaymentRequired,
            """{"error": {"type": "card_error", "code": "card_declined", "message": "Your card was declined."}}""");

        var exception = await Assert.ThrowsAsync<PaymentDeclinedException>(() => ChargeAsync(handler));

        Assert.Equal("Your card was declined.", exception.Message);
    }

    [Fact]
    public async Task Charge_WhenTheCardNeedsVerification_ThrowsDeclined()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"id": "pi_123", "object": "payment_intent", "status": "requires_action"}""");

        await Assert.ThrowsAsync<PaymentDeclinedException>(() => ChargeAsync(handler));
    }

    [Fact]
    public async Task Charge_WhenStripeFailsForAnotherReason_LetsTheErrorThrough()
    {
        var handler = new StubHandler(HttpStatusCode.Unauthorized,
            """{"error": {"type": "invalid_request_error", "message": "Invalid API Key provided."}}""");

        await Assert.ThrowsAsync<StripeException>(() => ChargeAsync(handler));
    }

    [Fact]
    public async Task Refund_RefundsThePaymentIntent()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"id": "re_123", "object": "refund", "status": "succeeded"}""");

        await Gateway(handler).RefundAsync("pi_123", TestContext.Current.CancellationToken);

        Assert.Equal("/v1/refunds", handler.Request!.RequestUri!.AbsolutePath);
        Assert.Contains("payment_intent=pi_123", handler.RequestBody);
    }
}
