using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace EventTicketing.E2ETests;

public class BookingTests : E2ETestBase
{
    [Test]
    public async Task SignedInUserBooksASeatAndSeesTheConfirmation()
    {
        // A throwaway event keeps the seeded events' seat counts unchanged for other tests.
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).Nth(1).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
        var created = await Page.APIRequest.PostAsync($"{BaseUrl}/api/v1/events", new()
        {
            DataObject = new
            {
                title = $"E2E Booking {Guid.NewGuid():N}"[..21],
                description = "Booked by an E2E test.",
                startTime = DateTime.UtcNow.AddDays(50),
                endTime = DateTime.UtcNow.AddDays(50).AddHours(2),
                venue = "Test Hall",
                eventType = "Other",
                totalSeats = 10,
                availableSeats = 10,
                priceCents = 500,
            },
        });
        var id = (await created.JsonAsync())!.Value.GetProperty("data").GetProperty("id").GetInt32();

        try
        {
            await Page.GotoAsync($"{BaseUrl}/events/{id}");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Buy ticket" }).ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Dialog).GetByLabel("Email")).ToHaveValueAsync("admin@example.com");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Place order" }).ClickAsync();

            await Expect(Page).ToHaveURLAsync(new Regex("/orders/\\d+$"));
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "You're going!" })).ToBeVisibleAsync();
            await Page.GotoAsync($"{BaseUrl}/events/{id}");
            await Expect(Page.GetByText("9 of 10 seats available")).ToBeVisibleAsync();
        }
        finally
        {
            await Page.APIRequest.DeleteAsync($"{BaseUrl}/api/v1/events/{id}");
        }
    }

    [Test]
    public async Task GuestBooksASeatAndOpensTheConfirmationWithItsLink()
    {
        // The admin creates a throwaway event, then signs out so the booking is a guest one.
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).Nth(1).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
        var title = $"E2E Guest {Guid.NewGuid():N}"[..19];
        var created = await Page.APIRequest.PostAsync($"{BaseUrl}/api/v1/events", new()
        {
            DataObject = new
            {
                title,
                description = "Booked by a guest in an E2E test.",
                startTime = DateTime.UtcNow.AddDays(51),
                endTime = DateTime.UtcNow.AddDays(51).AddHours(2),
                venue = "Test Hall",
                eventType = "Other",
                totalSeats = 10,
                availableSeats = 10,
                priceCents = 600,
            },
        });
        var id = (await created.JsonAsync())!.Value.GetProperty("data").GetProperty("id").GetInt32();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign out" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Link, new() { Name = "Sign in" })).ToBeVisibleAsync();

        try
        {
            await Page.GotoAsync($"{BaseUrl}/events/{id}");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Buy ticket" }).ClickAsync();
            await Page.GetByRole(AriaRole.Dialog).GetByLabel("Email").FillAsync("guest@example.com");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Place order" }).ClickAsync();

            await Expect(Page).ToHaveURLAsync(new Regex("/orders/\\d+\\?token=[A-Za-z0-9_-]+$"));
            await Expect(Page.GetByText(title)).ToBeVisibleAsync();

            // The guest link works again later; the bare order URL reveals nothing.
            var guestLink = Page.Url;
            await Page.GotoAsync(guestLink);
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "You're going!" })).ToBeVisibleAsync();
            await Page.GotoAsync(guestLink[..guestLink.IndexOf('?')]);
            await Expect(Page.GetByText("Order not found.")).ToBeVisibleAsync();
        }
        finally
        {
            await Page.GotoAsync($"{BaseUrl}/login");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).Nth(1).ClickAsync();
            await Expect(Page).ToHaveURLAsync(new Regex("/$"));
            await Page.APIRequest.DeleteAsync($"{BaseUrl}/api/v1/events/{id}");
        }
    }

    [Test]
    public async Task ADeclinedCardIsExplainedAndTheSeatStaysOnSale()
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).Nth(1).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
        var created = await Page.APIRequest.PostAsync($"{BaseUrl}/api/v1/events", new()
        {
            DataObject = new
            {
                title = $"E2E Declined {Guid.NewGuid():N}"[..22],
                description = "Paid for with a declined card in an E2E test.",
                startTime = DateTime.UtcNow.AddDays(52),
                endTime = DateTime.UtcNow.AddDays(52).AddHours(2),
                venue = "Test Hall",
                eventType = "Other",
                totalSeats = 10,
                availableSeats = 10,
                priceCents = 700,
            },
        });
        var id = (await created.JsonAsync())!.Value.GetProperty("data").GetProperty("id").GetInt32();

        try
        {
            // Without a Stripe key in the UI, the form offers Stripe's test cards; Stripe test
            // mode and the API's fake gateway both decline this one.
            await Page.GotoAsync($"{BaseUrl}/events/{id}");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Buy ticket" }).ClickAsync();
            var dialog = Page.GetByRole(AriaRole.Dialog);
            await dialog.GetByLabel("Visa 4000 0000 0000 0002 (declined)").CheckAsync();
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Place order" }).ClickAsync();

            await Expect(dialog.GetByRole(AriaRole.Alert)).ToHaveTextAsync("Your card was declined.");
            await Page.GotoAsync($"{BaseUrl}/events/{id}");
            await Expect(Page.GetByText("10 of 10 seats available")).ToBeVisibleAsync();
        }
        finally
        {
            await Page.APIRequest.DeleteAsync($"{BaseUrl}/api/v1/events/{id}");
        }
    }
}
