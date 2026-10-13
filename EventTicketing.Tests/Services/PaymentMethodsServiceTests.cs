using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Api.Payments;
using EventTicketing.Api.Services;
using EventTicketing.Tests.Fakes;
using EventTicketing.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Services;

[Trait("Category", "Integration")]
public class PaymentMethodsServiceTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private AppDbContext CreateDb() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    private async Task<int> CreateUserAsync()
    {
        var db = CreateDb();
        var user = new User { Name = "Card Holder", Email = $"{Guid.NewGuid():N}@example.com", PasswordHash = "unused" };
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user.Id;
    }

    private Task<PaymentMethodResponse> SaveAsync(RecordingPaymentGateway payments, int userId, string paymentMethodId = "pm_card_visa") =>
        new PaymentMethodsService(CreateDb(), payments).SaveDefaultAsync(
            userId, "holder@example.com", paymentMethodId, TestContext.Current.CancellationToken);

    private async Task<List<PaymentMethod>> RowsAsync(int userId) =>
        await CreateDb().PaymentMethods.Where(p => p.UserId == userId).OrderBy(p => p.Id).ToListAsync(TestContext.Current.CancellationToken);

    [Fact]
    public async Task SaveDefaultAsync_FirstCard_CreatesACustomerAndSavesOnlyIdsAndDisplayFields()
    {
        var userId = await CreateUserAsync();
        var payments = new RecordingPaymentGateway();

        var card = await SaveAsync(payments, userId);

        Assert.Equal(new PaymentMethodResponse("visa", "4242", 12, 2030), card);
        Assert.Equal((null, "holder@example.com", "pm_card_visa"), Assert.Single(payments.SavedCards));
        var row = Assert.Single(await RowsAsync(userId));
        Assert.Equal(("cus_test_1", "pm_card_visa_saved"), (row.StripeCustomerId, row.StripePaymentMethodId));
        Assert.True(row.IsDefault);
    }

    [Fact]
    public async Task SaveDefaultAsync_NewCard_ReplacesTheDefaultOnTheSameCustomer()
    {
        var userId = await CreateUserAsync();
        var payments = new RecordingPaymentGateway();
        await SaveAsync(payments, userId);

        await SaveAsync(payments, userId, "pm_card_mastercard");

        Assert.Equal("cus_test_1", payments.SavedCards[1].CustomerId);
        var rows = await RowsAsync(userId);
        Assert.Equal([false, true], rows.Select(r => r.IsDefault));
        Assert.Equal("pm_card_mastercard_saved", rows[1].StripePaymentMethodId);
    }

    [Fact]
    public async Task SaveDefaultAsync_WhenTheCardIsRefused_ThrowsAndKeepsTheOldCard()
    {
        var userId = await CreateUserAsync();
        var payments = new RecordingPaymentGateway();
        await SaveAsync(payments, userId);
        payments.DeclineSaveWith = "Your card was declined.";

        await Assert.ThrowsAsync<PaymentDeclinedException>(() => SaveAsync(payments, userId, "pm_card_chargeCustomerFail"));

        var row = Assert.Single(await RowsAsync(userId));
        Assert.True(row.IsDefault);
    }
}
