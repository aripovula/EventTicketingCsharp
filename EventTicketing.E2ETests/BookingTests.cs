using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace EventTicketing.E2ETests;

public class BookingTests : E2ETestBase
{
    // Login page buttons: 0 signs in the chosen demo user (John Doe by default), 1 the admin.
    private async Task SignInAsync(int demoPanelButton)
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).Nth(demoPanelButton).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
    }

    // Admins can't book, so tests that create an event as the admin buy their ticket as John.
    private async Task SwitchToCustomerAsync()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign out" }).ClickAsync();
        await SignInAsync(0);
    }

    private async Task DeleteAsAdminAsync(int id)
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign out" }).ClickAsync();
        await SignInAsync(1);
        await RemoveEventAsync(id);
    }

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
            await SwitchToCustomerAsync();
            await Page.GotoAsync($"{BaseUrl}/events/{id}");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Buy ticket" }).ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Dialog).GetByLabel("Email")).ToHaveValueAsync("john@example.com");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Place order" }).ClickAsync();

            await Expect(Page).ToHaveURLAsync(new Regex("/orders/\\d+$"));
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "You're going!" })).ToBeVisibleAsync();
            await Page.GotoAsync($"{BaseUrl}/events/{id}");
            await Expect(Page.GetByText("9 of 10 seats available")).ToBeVisibleAsync();
        }
        finally
        {
            await DeleteAsAdminAsync(id);
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
            await RemoveEventAsync(id);
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
            await SwitchToCustomerAsync();
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
            await DeleteAsAdminAsync(id);
        }
    }

    [Test]
    public async Task AUserSavesACardThenBooksWithIt()
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).Nth(1).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
        var created = await Page.APIRequest.PostAsync($"{BaseUrl}/api/v1/events", new()
        {
            DataObject = new
            {
                title = $"E2E Saved Card {Guid.NewGuid():N}"[..24],
                description = "Paid for with a saved card in an E2E test.",
                startTime = DateTime.UtcNow.AddDays(54),
                endTime = DateTime.UtcNow.AddDays(54).AddHours(2),
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
            // A new account, so the demo accounts' bookings in other tests never meet a saved card.
            var email = $"e2e-{Guid.NewGuid():N}@example.com";
            await Page.APIRequest.PostAsync($"{BaseUrl}/api/v1/auth/register", new() { DataObject = new { name = "Card Holder", email, password = "Password1" } });
            await Page.APIRequest.PostAsync($"{BaseUrl}/api/v1/auth/login", new() { DataObject = new { email, password = "Password1" } });

            await Page.GotoAsync($"{BaseUrl}/profile");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Save card" }).ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Status)).ToHaveTextAsync("Card saved.");
            await Expect(Page.GetByText("Visa ending in 4242")).ToBeVisibleAsync();

            await Page.GotoAsync($"{BaseUrl}/events/{id}");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Buy ticket" }).ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Dialog).GetByLabel(new Regex("^Visa ending in 4242"))).ToBeCheckedAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Place order" }).ClickAsync();

            await Expect(Page).ToHaveURLAsync(new Regex("/orders/\\d+$"));
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "You're going!" })).ToBeVisibleAsync();
        }
        finally
        {
            await Page.GotoAsync($"{BaseUrl}/login");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).Nth(1).ClickAsync();
            await Expect(Page).ToHaveURLAsync(new Regex("/$"));
            await RemoveEventAsync(id);
        }
    }
}
