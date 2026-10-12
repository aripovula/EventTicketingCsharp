using EventTicketing.Api.Payments;

namespace EventTicketing.Tests.Payments;

public class FakePaymentGatewayTests
{
    [Fact]
    public async Task Charge_ApprovesAnyOtherCard()
    {
        var paymentId = await new FakePaymentGateway().ChargeAsync(
            2500, FakePaymentGateway.ApprovedPaymentMethod, "key-1", TestContext.Current.CancellationToken);

        Assert.StartsWith("pi_fake_", paymentId);
    }

    [Fact]
    public async Task Charge_DeclinesStripesDeclineTestCard()
    {
        await Assert.ThrowsAsync<PaymentDeclinedException>(() => new FakePaymentGateway().ChargeAsync(
            2500, FakePaymentGateway.DeclinedPaymentMethod, "key-1", TestContext.Current.CancellationToken));
    }
}
