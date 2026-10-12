using Microsoft.Extensions.Options;
using Stripe;

namespace EventTicketing.Api.Payments;

public class StripePaymentGateway(IStripeClient stripe, IOptions<PaymentsOptions> options) : IPaymentGateway
{
    public async Task<string> ChargeAsync(int amountCents, string paymentMethodId, string idempotencyKey, CancellationToken cancellationToken)
    {
        PaymentIntent intent;
        try
        {
            intent = await new PaymentIntentService(stripe).CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = amountCents,
                Currency = options.Value.Currency,
                PaymentMethod = paymentMethodId,
                PaymentMethodTypes = ["card"],
                // Create and confirm in one call: the card is charged now, or the call fails.
                Confirm = true,
            }, new RequestOptions { IdempotencyKey = idempotencyKey }, cancellationToken);
        }
        catch (StripeException e) when (e.StripeError?.Type == "card_error")
        {
            throw new PaymentDeclinedException(e.StripeError.Message ?? "Your card was declined.");
        }

        // A card that needs 3-D Secure comes back "requires_action"; we don't run that browser step,
        // and an unconfirmed intent charges nothing.
        if (intent.Status != "succeeded")
            throw new PaymentDeclinedException("This card needs extra verification, which we don't support yet. Please try another card.");

        return intent.Id;
    }

    public Task RefundAsync(string paymentId, CancellationToken cancellationToken) =>
        new RefundService(stripe).CreateAsync(new RefundCreateOptions { PaymentIntent = paymentId }, cancellationToken: cancellationToken);
}
