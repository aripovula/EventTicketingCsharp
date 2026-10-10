using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace EventTicketing.E2ETests;

public class LiveUpdatesTests : E2ETestBase
{
    [Test]
    public async Task ABookingInOneBrowserUpdatesTheSeatsInAnother()
    {
        // The admin creates a throwaway event so seeded seat counts stay unchanged.
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).Nth(1).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
        var created = await Page.APIRequest.PostAsync($"{BaseUrl}/api/v1/events", new()
        {
            DataObject = new
            {
                title = $"E2E Live {Guid.NewGuid():N}"[..18],
                description = "Watched live by an E2E test.",
                startTime = DateTime.UtcNow.AddDays(52),
                endTime = DateTime.UtcNow.AddDays(52).AddHours(2),
                venue = "Test Hall",
                eventType = "Other",
                totalSeats = 10,
                availableSeats = 10,
                priceCents = 800,
            },
        });
        var id = (await created.JsonAsync())!.Value.GetProperty("data").GetProperty("id").GetInt32();

        // A visitor in a second, separate browser context watches the event page.
        await using var visitorContext = await Browser.NewContextAsync();
        var visitor = await visitorContext.NewPageAsync();
        try
        {
            await visitor.GotoAsync($"{BaseUrl}/events/{id}");
            await Expect(visitor.GetByText("10 of 10 seats available")).ToBeVisibleAsync(new() { Timeout = 15_000 });

            await Page.GotoAsync($"{BaseUrl}/events/{id}");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Buy ticket" }).ClickAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Place order" }).ClickAsync();
            await Expect(Page).ToHaveURLAsync(new Regex("/orders/\\d+$"));

            await Expect(visitor.GetByText("9 of 10 seats available")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        }
        finally
        {
            await Page.APIRequest.DeleteAsync($"{BaseUrl}/api/v1/events/{id}");
        }
    }
}
