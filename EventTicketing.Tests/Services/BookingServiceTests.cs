using System.Text.Json;
using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
using EventTicketing.Api.Messaging;
using EventTicketing.Api.Models;
using EventTicketing.Api.Payments;
using EventTicketing.Api.Services;
using EventTicketing.Tests.Fakes;
using EventTicketing.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EventTicketing.Tests.Services;

[Trait("Category", "Integration")]
public class BookingServiceTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private AppDbContext CreateDb() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    private BookingService CreateService(IPaymentGateway? payments = null, ILogger<BookingService>? logger = null)
    {
        var db = CreateDb();
        return new(db, Options.Create(new RabbitMqOptions()), new AuditLogger(db), payments ?? new RecordingPaymentGateway(),
            logger ?? NullLogger<BookingService>.Instance);
    }

    private static BookingRequest Request(string email) =>
        new() { Email = email, PaymentMethodId = FakePaymentGateway.ApprovedPaymentMethod };

    private async Task<int> CreateEventAsync(int availableSeats, int priceCents = 2500)
    {
        var db = CreateDb();
        var ev = new Event
        {
            Title = $"Booking Test {Guid.NewGuid():N}"[..20],
            Description = "For booking tests.",
            StartTime = DateTime.UtcNow.AddDays(5),
            EndTime = DateTime.UtcNow.AddDays(5).AddHours(2),
            Venue = "Test Hall",
            TotalSeats = 10,
            AvailableSeats = availableSeats,
            PriceCents = priceCents,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return ev.Id;
    }

    private async Task<List<(OutboxMessage Row, BookingConfirmedMessage Message)>> QueuedConfirmationsAsync(int eventId) =>
        (await CreateDb().OutboxMessages
            .Where(m => m.Type == nameof(BookingConfirmedMessage))
            .ToListAsync(TestContext.Current.CancellationToken))
        .Select(row => (row, JsonSerializer.Deserialize<BookingConfirmedMessage>(row.Payload)!))
        .Where(queued => queued.Item2.EventId == eventId)
        .ToList();

    private async Task<int> AvailableSeatsAsync(int eventId) =>
        (await CreateDb().Events.SingleAsync(e => e.Id == eventId, TestContext.Current.CancellationToken)).AvailableSeats;

    private Task<BookingResult> BookAsync(int eventId, IPaymentGateway? payments = null) =>
        CreateService(payments).BookAsync(
            eventId, Request("guest@example.com"), null, "booking-key", TestContext.Current.CancellationToken);

    [Fact]
    public async Task BookAsync_TakesOneSeat()
    {
        var eventId = await CreateEventAsync(availableSeats: 3);

        var result = await BookAsync(eventId);

        Assert.Equal(BookingStatus.Booked, result.Status);
        Assert.Equal(2, await AvailableSeatsAsync(eventId));
    }

    [Fact]
    public async Task BookAsync_WhenSoldOut_ReportsSoldOutAndLeavesSeatsAtZero()
    {
        var eventId = await CreateEventAsync(availableSeats: 0);

        var result = await BookAsync(eventId);

        Assert.Equal(BookingStatus.SoldOut, result.Status);
        Assert.Equal(0, await AvailableSeatsAsync(eventId));
    }

    [Fact]
    public async Task BookAsync_UnknownEvent_ReportsEventNotFound()
    {
        var result = await BookAsync(999_999);

        Assert.Equal(BookingStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task BookAsync_SavesTheOrderAtTheEventPrice()
    {
        var eventId = await CreateEventAsync(availableSeats: 5);
        var john = await CreateDb().Users.SingleAsync(u => u.Email == "john@example.com", TestContext.Current.CancellationToken);

        var result = await CreateService().BookAsync(
            eventId, Request("john@example.com"), john.Id, "booking-key", TestContext.Current.CancellationToken);

        Assert.NotNull(result.Order);
        Assert.Equal((eventId, "john@example.com", 2500), (result.Order.EventId, result.Order.Email, result.Order.PriceCents));
        var saved = await CreateDb().Orders.SingleAsync(o => o.Id == result.Order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(john.Id, saved.UserId);
    }

    [Fact]
    public async Task BookAsync_GuestOrderHasNoUser()
    {
        var eventId = await CreateEventAsync(availableSeats: 5);

        var result = await BookAsync(eventId);

        var saved = await CreateDb().Orders.SingleAsync(o => o.Id == result.Order!.Id, TestContext.Current.CancellationToken);
        Assert.Null(saved.UserId);
        Assert.Equal("guest@example.com", saved.Email);
    }

    [Fact]
    public async Task BookAsync_GivesGuestsATokenAndStoresOnlyItsHash()
    {
        var eventId = await CreateEventAsync(availableSeats: 5);

        var result = await BookAsync(eventId);

        var token = result.Order!.GuestAccessToken;
        Assert.False(string.IsNullOrEmpty(token));
        var saved = await CreateDb().Orders.SingleAsync(o => o.Id == result.Order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(SecureToken.Hash(token), saved.GuestAccessTokenHash);
    }

    [Fact]
    public async Task BookAsync_SignedInBookingsGetNoGuestToken()
    {
        var eventId = await CreateEventAsync(availableSeats: 5);
        var john = await CreateDb().Users.SingleAsync(u => u.Email == "john@example.com", TestContext.Current.CancellationToken);

        var result = await CreateService().BookAsync(
            eventId, Request("john@example.com"), john.Id, "booking-key", TestContext.Current.CancellationToken);

        Assert.Null(result.Order!.GuestAccessToken);
        var saved = await CreateDb().Orders.SingleAsync(o => o.Id == result.Order.Id, TestContext.Current.CancellationToken);
        Assert.Null(saved.GuestAccessTokenHash);
    }

    [Fact]
    public async Task BookAsync_QueuesABookingConfirmedMessageWithTheOrder()
    {
        var eventId = await CreateEventAsync(availableSeats: 5);

        var result = await BookAsync(eventId);

        var (row, message) = Assert.Single(await QueuedConfirmationsAsync(eventId));
        Assert.Equal((result.Order!.Id, "guest@example.com", 2500), (message.OrderId, message.Email, message.PriceCents));
        Assert.Equal(("ticketing", "booking.confirmed"), (row.Exchange, row.RoutingKey));
    }

    [Fact]
    public async Task BookAsync_WhenSoldOut_QueuesNoMessage()
    {
        var eventId = await CreateEventAsync(availableSeats: 0);

        await BookAsync(eventId);

        Assert.Empty(await QueuedConfirmationsAsync(eventId));
    }

    [Fact]
    public async Task BookAsync_RecordsTheBooking()
    {
        var eventId = await CreateEventAsync(availableSeats: 3);

        var result = await BookAsync(eventId);

        var entry = await CreateDb().AuditLogs.SingleAsync(
            a => a.Action == AuditAction.TicketBooked && a.EntityId == result.Order!.Id, TestContext.Current.CancellationToken);
        Assert.Equal(("Order", "guest@example.com"), (entry.EntityType, entry.UserEmail));
        Assert.Null(entry.UserId);
        Assert.Equal(eventId, JsonDocument.Parse(entry.Details!).RootElement.GetProperty("eventId").GetInt32());
    }

    [Fact]
    public async Task BookAsync_ChargesTheEventPriceWithTheIdempotencyKey()
    {
        var eventId = await CreateEventAsync(availableSeats: 3);
        var payments = new RecordingPaymentGateway();

        var result = await BookAsync(eventId, payments);

        Assert.Equal((2500, FakePaymentGateway.ApprovedPaymentMethod, (string?)null, "booking-key"), Assert.Single(payments.Charges));
        var saved = await CreateDb().Orders.SingleAsync(o => o.Id == result.Order!.Id, TestContext.Current.CancellationToken);
        Assert.Equal("pi_test_1", saved.StripePaymentIntentId);
    }

    [Fact]
    public async Task BookAsync_WhenSoldOut_NeverCharges()
    {
        var eventId = await CreateEventAsync(availableSeats: 0);
        var payments = new RecordingPaymentGateway();

        await BookAsync(eventId, payments);

        Assert.Empty(payments.Charges);
    }

    [Fact]
    public async Task BookAsync_WhenTheCardIsDeclined_PutsTheSeatBackAndSavesNoOrder()
    {
        var eventId = await CreateEventAsync(availableSeats: 3);

        var result = await BookAsync(eventId, new RecordingPaymentGateway { DeclineWith = "Your card was declined." });

        Assert.Equal((BookingStatus.PaymentDeclined, "Your card was declined."), (result.Status, result.Error));
        Assert.Equal(3, await AvailableSeatsAsync(eventId));
        Assert.False(await CreateDb().Orders.AnyAsync(o => o.EventId == eventId, TestContext.Current.CancellationToken));
        Assert.Empty(await QueuedConfirmationsAsync(eventId));
    }

    [Fact]
    public async Task BookAsync_WhenTheChargeFails_PutsTheSeatBackAndRethrows()
    {
        var eventId = await CreateEventAsync(availableSeats: 3);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            BookAsync(eventId, new RecordingPaymentGateway { FailWith = new HttpRequestException("Stripe is down") }));

        Assert.Equal(3, await AvailableSeatsAsync(eventId));
    }

    // Longer than the email column, so saving the order fails after the card was charged.
    private static BookingRequest UnsaveableRequest() => Request(new string('a', 200) + "@example.com");

    [Fact]
    public async Task BookAsync_WhenTheOrderCantBeSaved_RefundsAndPutsTheSeatBack()
    {
        var eventId = await CreateEventAsync(availableSeats: 3);
        var payments = new RecordingPaymentGateway();

        await Assert.ThrowsAsync<DbUpdateException>(() => CreateService(payments).BookAsync(
            eventId, UnsaveableRequest(), null, "booking-key", TestContext.Current.CancellationToken));

        Assert.Equal(["pi_test_1"], payments.Refunds);
        Assert.Equal(3, await AvailableSeatsAsync(eventId));
    }

    [Fact]
    public async Task BookAsync_WhenTheRefundFailsToo_LogsItAndThrowsTheOriginalError()
    {
        var eventId = await CreateEventAsync(availableSeats: 3);
        var payments = new RecordingPaymentGateway { RefundFailsWith = new HttpRequestException("Stripe is down") };
        var logger = new CollectingLogger<BookingService>();

        await Assert.ThrowsAsync<DbUpdateException>(() => CreateService(payments, logger).BookAsync(
            eventId, UnsaveableRequest(), null, "booking-key", TestContext.Current.CancellationToken));

        Assert.Contains(logger.Messages, m => m.Contains("Refund of payment pi_test_1 failed"));
        Assert.Equal(3, await AvailableSeatsAsync(eventId));
    }

    [Fact]
    public async Task BookAsync_ForAFreeEvent_BooksWithoutACard()
    {
        var eventId = await CreateEventAsync(availableSeats: 3, priceCents: 0);
        var payments = new RecordingPaymentGateway();

        var result = await CreateService(payments).BookAsync(
            eventId, new BookingRequest { Email = "guest@example.com" }, null, "booking-key", TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Booked, result.Status);
        Assert.Empty(payments.Charges);
        var saved = await CreateDb().Orders.SingleAsync(o => o.Id == result.Order!.Id, TestContext.Current.CancellationToken);
        Assert.Null(saved.StripePaymentIntentId);
        Assert.Equal(2, await AvailableSeatsAsync(eventId));
    }

    [Fact]
    public async Task BookAsync_ForAPaidEventWithoutACard_AsksForOneAndKeepsTheSeat()
    {
        var eventId = await CreateEventAsync(availableSeats: 3);

        var result = await CreateService().BookAsync(
            eventId, new BookingRequest { Email = "guest@example.com" }, null, "booking-key", TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.PaymentMethodRequired, result.Status);
        Assert.Equal(3, await AvailableSeatsAsync(eventId));
    }

    private async Task<int> CreateUserWithSavedCardAsync()
    {
        var db = CreateDb();
        var user = new User { Name = "Card Holder", Email = $"{Guid.NewGuid():N}@example.com", PasswordHash = "unused" };
        db.Users.Add(user);
        db.PaymentMethods.Add(new PaymentMethod
        {
            User = user,
            StripeCustomerId = "cus_saved",
            StripePaymentMethodId = "pm_saved",
            Brand = "visa",
            Last4 = "4242",
            ExpMonth = 12,
            ExpYear = 2030,
            IsDefault = true,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user.Id;
    }

    [Fact]
    public async Task BookAsync_WithTheSavedCard_ChargesItThroughItsCustomer()
    {
        var eventId = await CreateEventAsync(availableSeats: 3);
        var userId = await CreateUserWithSavedCardAsync();
        var payments = new RecordingPaymentGateway();

        var result = await CreateService(payments).BookAsync(
            eventId, new BookingRequest { Email = "holder@example.com", UseSavedPaymentMethod = true }, userId, "booking-key",
            TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Booked, result.Status);
        Assert.Equal((2500, "pm_saved", "cus_saved", "booking-key"), Assert.Single(payments.Charges));
    }

    [Fact]
    public async Task BookAsync_WithTheSavedCardButNoneSaved_ReportsItAndKeepsTheSeat()
    {
        var eventId = await CreateEventAsync(availableSeats: 3);
        var payments = new RecordingPaymentGateway();

        // A guest has no saved card either.
        var result = await CreateService(payments).BookAsync(
            eventId, new BookingRequest { Email = "guest@example.com", UseSavedPaymentMethod = true }, null, "booking-key",
            TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.NoSavedPaymentMethod, result.Status);
        Assert.Empty(payments.Charges);
        Assert.Equal(3, await AvailableSeatsAsync(eventId));
    }
}
