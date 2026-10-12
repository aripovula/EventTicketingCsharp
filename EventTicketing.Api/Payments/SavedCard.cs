namespace EventTicketing.Api.Payments;

/// <summary>A card attached to a Stripe customer, with what we show the user about it.</summary>
public record SavedCard(string CustomerId, string PaymentMethodId, string Brand, string Last4, int ExpMonth, int ExpYear);
