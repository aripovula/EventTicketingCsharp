namespace EventTicketing.Api.Payments;

public class StripeOptions
{
    public const string SectionName = "Stripe";

    // A secret: user-secrets locally, Key Vault when deployed; never appsettings.
    public string SecretKey { get; set; } = string.Empty;
}
