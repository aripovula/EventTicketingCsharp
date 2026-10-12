namespace EventTicketing.Api.Payments;

public class PaymentsOptions
{
    public const string SectionName = "Payments";

    public const string StripeProvider = "Stripe";

    public const string FakeProvider = "Fake";

    /// <summary>
    /// "Stripe" charges cards through Stripe; "Fake" approves every card except Stripe's decline
    /// test id without calling Stripe, for E2E runs without Stripe keys. Fake is refused in Production.
    /// </summary>
    public string Provider { get; set; } = StripeProvider;

    public string Currency { get; set; } = "usd";
}
