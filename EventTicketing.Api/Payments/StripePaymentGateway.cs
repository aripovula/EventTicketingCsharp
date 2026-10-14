using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Stripe;

namespace EventTicketing.Api.Payments;

public class StripePaymentGateway(IStripeClient stripe, IOptions<PaymentsOptions> options) : IPaymentGateway
{
    public async Task<string> ChargeAsync(int amountCents, string paymentMethodId, string? customerId, string idempotencyKey, CancellationToken cancellationToken)
    {
        PaymentIntent intent;
        try
        {
            intent = await new PaymentIntentService(stripe).CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = amountCents,
                Currency = options.Value.Currency,
                PaymentMethod = paymentMethodId,
                // Stripe only charges a saved card together with the customer it belongs to.
                Customer = customerId,
                PaymentMethodTypes = ["card"],
                // Create and confirm in one call: the card is charged now, or the call fails.
                Confirm = true,
            }, new RequestOptions { IdempotencyKey = idempotencyKey }, cancellationToken);
        }
        catch (StripeException e) when (e.StripeError?.Type == "card_error")
        {
            throw new PaymentDeclinedException(e.StripeError.Message ?? "Your card was declined.");
        }
        catch (BrokenCircuitException e)
        {
            throw Unavailable(e);
        }

        // A card that needs 3-D Secure comes back "requires_action"; we don't run that browser step,
        // and an unconfirmed intent charges nothing.
        if (intent.Status != "succeeded")
            throw new PaymentDeclinedException("This card needs extra verification, which we don't support yet. Please try another card.");

        return intent.Id;
    }

    public Task RefundAsync(string paymentId, CancellationToken cancellationToken) =>
        new RefundService(stripe).CreateAsync(new RefundCreateOptions { PaymentIntent = paymentId }, cancellationToken: cancellationToken);

    public async Task<SavedCard> SaveCardAsync(string? customerId, string email, string paymentMethodId, CancellationToken cancellationToken)
    {
        customerId ??= (await new CustomerService(stripe).CreateAsync(
            new CustomerCreateOptions { Email = email }, cancellationToken: cancellationToken)).Id;

        PaymentMethod method;
        try
        {
            method = await new PaymentMethodService(stripe).AttachAsync(
                paymentMethodId, new PaymentMethodAttachOptions { Customer = customerId }, cancellationToken: cancellationToken);
        }
        catch (StripeException e) when (e.StripeError?.Type == "card_error")
        {
            throw new PaymentDeclinedException(e.StripeError.Message ?? "Your card was declined.");
        }

        return new SavedCard(customerId, method.Id, method.Card.Brand, method.Card.Last4, (int)method.Card.ExpMonth, (int)method.Card.ExpYear);
    }

    // The breaker opened because Stripe kept failing; it lets a trial call through after its break.
    private static PaymentsUnavailableException Unavailable(BrokenCircuitException e) => new(StripeHttpClient.BreakDuration, e);
}
