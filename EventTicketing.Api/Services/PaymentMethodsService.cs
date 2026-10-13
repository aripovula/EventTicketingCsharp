using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Api.Payments;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Services;

public class PaymentMethodsService(AppDbContext db, IPaymentGateway payments)
{
    /// <summary>Saves the card to the user's Stripe customer and makes it the card bookings use.</summary>
    /// <exception cref="PaymentDeclinedException">Stripe refused the card.</exception>
    public async Task<PaymentMethodResponse> SaveDefaultAsync(int userId, string email, string paymentMethodId, CancellationToken cancellationToken)
    {
        // Every card of a user goes on the same Stripe customer, created with the first one.
        var current = await db.PaymentMethods.SingleOrDefaultAsync(p => p.UserId == userId && p.IsDefault, cancellationToken);
        var card = await payments.SaveCardAsync(current?.StripeCustomerId, email, paymentMethodId, cancellationToken);

        if (current is not null)
            current.IsDefault = false;
        db.PaymentMethods.Add(new PaymentMethod
        {
            UserId = userId,
            StripeCustomerId = card.CustomerId,
            StripePaymentMethodId = card.PaymentMethodId,
            Brand = card.Brand,
            Last4 = card.Last4,
            ExpMonth = card.ExpMonth,
            ExpYear = card.ExpYear,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);

        return new PaymentMethodResponse(card.Brand, card.Last4, card.ExpMonth, card.ExpYear);
    }
}
