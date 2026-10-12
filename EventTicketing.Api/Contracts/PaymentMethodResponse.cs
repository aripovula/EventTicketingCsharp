namespace EventTicketing.Api.Contracts;

/// <param name="Brand">Stripe's lowercase brand name, e.g. "visa".</param>
public record PaymentMethodResponse(string Brand, string Last4, int ExpMonth, int ExpYear);
