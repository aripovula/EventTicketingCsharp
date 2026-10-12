namespace EventTicketing.Api.Payments;

/// <summary>The card was refused; the message is safe to show the buyer.</summary>
public class PaymentDeclinedException(string message) : Exception(message);
