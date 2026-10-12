using System.Net;
using System.Text;
using EventTicketing.Api.Payments;
using Microsoft.Extensions.Options;
using Stripe;

namespace EventTicketing.Tests.Payments;

public class StripePaymentGatewayTests
{
    // Answers Stripe API calls with canned responses, in order (the last one repeats), and remembers the requests.
    private sealed class StubHandler(HttpStatusCode status, params string[] bodies) : HttpMessageHandler
    {
        public List<(HttpRequestMessage Request, string? Body)> Calls { get; } = [];

        public HttpRequestMessage? Request => Calls[^1].Request;

        public string? RequestBody => Calls[^1].Body;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Calls.Add((request, body));
            var response = bodies[Math.Min(Calls.Count, bodies.Length) - 1];
            return new HttpResponseMessage(status) { Content = new StringContent(response, Encoding.UTF8, "application/json") };
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

    [Fact]
    public async Task Charge_WithASavedCard_ChargesItThroughItsCustomer()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"id": "pi_123", "object": "payment_intent", "status": "succeeded"}""");

        await Gateway(handler).ChargeAsync(2500, "pm_saved", "cus_123", "key-123", TestContext.Current.CancellationToken);

        var form = WebUtility.UrlDecode(handler.RequestBody!);
        Assert.Contains("payment_method=pm_saved", form);
        Assert.Contains("customer=cus_123", form);
    }

    private const string AttachedCard = """
        {"id": "pm_saved", "object": "payment_method", "type": "card",
         "card": {"brand": "visa", "last4": "4242", "exp_month": 12, "exp_year": 2030}}
        """;

    [Fact]
    public async Task SaveCard_ForANewCustomer_CreatesTheCustomerThenAttachesTheCard()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"id": "cus_new", "object": "customer"}""", AttachedCard);

        var card = await Gateway(handler).SaveCardAsync(null, "jane@example.com", "pm_card_visa", TestContext.Current.CancellationToken);

        Assert.Equal(new SavedCard("cus_new", "pm_saved", "visa", "4242", 12, 2030), card);
        Assert.Equal("/v1/customers", handler.Calls[0].Request.RequestUri!.AbsolutePath);
        Assert.Contains("email=jane@example.com", WebUtility.UrlDecode(handler.Calls[0].Body!));
        Assert.Equal("/v1/payment_methods/pm_card_visa/attach", handler.Calls[1].Request.RequestUri!.AbsolutePath);
        Assert.Contains("customer=cus_new", handler.Calls[1].Body);
    }

    [Fact]
    public async Task SaveCard_ForAnExistingCustomer_OnlyAttachesTheCard()
    {
        var handler = new StubHandler(HttpStatusCode.OK, AttachedCard);

        var card = await Gateway(handler).SaveCardAsync("cus_123", "jane@example.com", "pm_card_visa", TestContext.Current.CancellationToken);

        Assert.Equal("cus_123", card.CustomerId);
        var call = Assert.Single(handler.Calls);
        Assert.Equal("/v1/payment_methods/pm_card_visa/attach", call.Request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task SaveCard_WhenTheCardIsRefused_ThrowsWithStripesMessage()
    {
        var handler = new StubHandler(HttpStatusCode.PaymentRequired,
            """{"error": {"type": "card_error", "code": "card_declined", "message": "Your card was declined."}}""");

        var exception = await Assert.ThrowsAsync<PaymentDeclinedException>(() =>
            Gateway(handler).SaveCardAsync("cus_123", "jane@example.com", "pm_card_visa", TestContext.Current.CancellationToken));

        Assert.Equal("Your card was declined.", exception.Message);
    }
}
