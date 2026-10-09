using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Api.Services;
using EventTicketing.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Services;

[Trait("Category", "Integration")]
public class OrdersServiceTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private AppDbContext CreateDb() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    private async Task<int> UserIdAsync(string email) =>
        (await CreateDb().Users.SingleAsync(u => u.Email == email, TestContext.Current.CancellationToken)).Id;

    private async Task<OrderResponse> BookAsync(int? userId, string email)
    {
        var db = CreateDb();
        var ev = new Event
        {
            Title = $"Orders Test {Guid.NewGuid():N}"[..19],
            Description = "For order tests.",
            StartTime = DateTime.UtcNow.AddDays(5),
            EndTime = DateTime.UtcNow.AddDays(5).AddHours(2),
            Venue = "Test Hall",
            TotalSeats = 10,
            AvailableSeats = 10,
            PriceCents = 2500,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var result = await new BookingService(CreateDb()).BookAsync(ev.Id, new BookingRequest { Email = email }, userId, TestContext.Current.CancellationToken);
        return result.Order!;
    }

    private Task<OrderResponse?> GetAsync(int orderId, OrderViewer viewer) =>
        new OrdersService(CreateDb()).GetAsync(orderId, viewer, TestContext.Current.CancellationToken);

    [Fact]
    public async Task TheOwnerSeesTheirOrder()
    {
        var john = await UserIdAsync("john@example.com");
        var order = await BookAsync(john, "john@example.com");

        var seen = await GetAsync(order.Id, new OrderViewer(john, false, null));

        Assert.NotNull(seen);
        Assert.Equal((order.Id, order.EventTitle, 2500), (seen.Id, seen.EventTitle, seen.PriceCents));
    }

    [Fact]
    public async Task AnotherUserCannotSeeIt()
    {
        var order = await BookAsync(await UserIdAsync("john@example.com"), "john@example.com");

        Assert.Null(await GetAsync(order.Id, new OrderViewer(await UserIdAsync("jane@example.com"), false, null)));
    }

    [Fact]
    public async Task AnAdminSeesAnyOrder()
    {
        var order = await BookAsync(await UserIdAsync("john@example.com"), "john@example.com");

        Assert.NotNull(await GetAsync(order.Id, new OrderViewer(await UserIdAsync("admin@example.com"), true, null)));
    }

    [Fact]
    public async Task AGuestSeesTheirOrderWithTheToken()
    {
        var order = await BookAsync(null, "guest@example.com");

        var seen = await GetAsync(order.Id, new OrderViewer(null, false, order.GuestAccessToken));

        Assert.NotNull(seen);
        Assert.Null(seen.GuestAccessToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-token")]
    public async Task AGuestOrderIsHiddenWithoutTheRightToken(string? token)
    {
        var order = await BookAsync(null, "guest@example.com");

        Assert.Null(await GetAsync(order.Id, new OrderViewer(null, false, token)));
    }

    [Fact]
    public async Task AnAnonymousViewerCannotSeeAUsersOrder()
    {
        var order = await BookAsync(await UserIdAsync("john@example.com"), "john@example.com");

        Assert.Null(await GetAsync(order.Id, new OrderViewer(null, false, null)));
    }

    [Fact]
    public async Task AMissingOrderIsNull()
    {
        Assert.Null(await GetAsync(999_999, new OrderViewer(null, true, null)));
    }
}
