using EventTicketing.Api.Payments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class PaymentsOptionsStartupTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private IPaymentGateway GatewayWith(string provider, string secretKey)
    {
        var host = factory.WithWebHostBuilder(builder => builder
            .UseSetting("Payments:Provider", provider)
            .UseSetting("Stripe:SecretKey", secretKey));
        return host.Services.CreateScope().ServiceProvider.GetRequiredService<IPaymentGateway>();
    }

    [Fact]
    public void TheFakeProvider_UsesTheFakeGateway()
    {
        Assert.IsType<FakePaymentGateway>(GatewayWith("Fake", ""));
    }

    [Fact]
    public void TheStripeProvider_UsesTheStripeGateway()
    {
        Assert.IsType<StripePaymentGateway>(GatewayWith("Stripe", "sk_test_configured"));
    }

    [Fact]
    public void StartupFailsWhenTheStripeKeyIsMissing()
    {
        var exception = Assert.Throws<OptionsValidationException>(() => GatewayWith("Stripe", ""));

        Assert.Contains("Stripe:SecretKey is required", exception.Message);
    }

    [Fact]
    public void StartupFailsForAnUnknownProvider()
    {
        var exception = Assert.Throws<OptionsValidationException>(() => GatewayWith("PayPal", ""));

        Assert.Contains("Payments:Provider must be Stripe or Fake", exception.Message);
    }

    [Fact]
    public void StartupFailsForTheFakeProviderInProduction()
    {
        var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        var exception = Assert.Throws<OptionsValidationException>(() => production.CreateClient());

        Assert.Contains("Payments:Provider=Fake is not allowed in Production", exception.Message);
    }
}
