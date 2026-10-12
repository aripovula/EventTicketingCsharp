using EventTicketing.Api.Payments;

namespace EventTicketing.Tests.Payments;

public class FakePaymentGatewayTests
{
    [Fact]
    public async Task Charge_ApprovesAnyOtherCard()
    {
        var paymentId = await new FakePaymentGateway().ChargeAsync(
            2500, FakePaymentGateway.ApprovedPaymentMethod, null, "key-1", TestContext.Current.CancellationToken);

        Assert.StartsWith("pi_fake_", paymentId);
    }

    [Fact]
    public async Task Charge_DeclinesStripesDeclineTestCard()
    {
        await Assert.ThrowsAsync<PaymentDeclinedException>(() => new FakePaymentGateway().ChargeAsync(
            2500, FakePaymentGateway.DeclinedPaymentMethod, null, "key-1", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveCard_CreatesACustomerAndKeepsTheTestId()
    {
        var card = await new FakePaymentGateway().SaveCardAsync(
            null, "jane@example.com", FakePaymentGateway.ApprovedPaymentMethod, TestContext.Current.CancellationToken);

        Assert.StartsWith("cus_fake_", card.CustomerId);
        Assert.Equal(FakePaymentGateway.ApprovedPaymentMethod, card.PaymentMethodId);
        Assert.Equal(("visa", "4242"), (card.Brand, card.Last4));
    }

    [Fact]
    public async Task SaveCard_KeepsAnExistingCustomer()
    {
        var card = await new FakePaymentGateway().SaveCardAsync(
            "cus_123", "jane@example.com", FakePaymentGateway.DeclinedPaymentMethod, TestContext.Current.CancellationToken);

        Assert.Equal("cus_123", card.CustomerId);
        Assert.Equal("0002", card.Last4);
    }
}
