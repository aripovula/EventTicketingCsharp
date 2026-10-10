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
}
