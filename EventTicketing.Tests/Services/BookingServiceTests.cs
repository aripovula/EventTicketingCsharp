using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Api.Services;
using EventTicketing.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Services;

[Trait("Category", "Integration")]
public class BookingServiceTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private AppDbContext CreateDb() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    private async Task<int> CreateEventAsync(int availableSeats)
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
            PriceCents = 2500,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return ev.Id;
    }

    private async Task<int> AvailableSeatsAsync(int eventId) =>
        (await CreateDb().Events.SingleAsync(e => e.Id == eventId, TestContext.Current.CancellationToken)).AvailableSeats;

    private Task<BookingResult> BookAsync(int eventId) =>
        new BookingService(CreateDb()).BookAsync(
            eventId, new BookingRequest { Email = "guest@example.com" }, null, TestContext.Current.CancellationToken);

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

        var result = await new BookingService(CreateDb()).BookAsync(
            eventId, new BookingRequest { Email = "john@example.com" }, john.Id, TestContext.Current.CancellationToken);

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
}
