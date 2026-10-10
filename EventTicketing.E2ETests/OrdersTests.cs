using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace EventTicketing.E2ETests;

public class OrdersTests : E2ETestBase
{
    private async Task SignInAsync(int demoPanelButton)
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).Nth(demoPanelButton).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
    }

    [Test]
    public async Task ABookedOrderAppearsUnderMyOrders()
    {
        // The admin creates a throwaway event so seeded seat counts stay unchanged.
        await SignInAsync(1);
        var title = $"E2E Orders {Guid.NewGuid():N}"[..20];
        var created = await Page.APIRequest.PostAsync($"{BaseUrl}/api/v1/events", new()
        {
            DataObject = new
            {
                title,
                description = "Booked by an E2E test.",
                startTime = DateTime.UtcNow.AddDays(55),
                endTime = DateTime.UtcNow.AddDays(55).AddHours(2),
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
            await Page.GetByRole(AriaRole.Button, new() { Name = "Sign out" }).ClickAsync();
            await Page.GetByRole(AriaRole.Radio, new() { Name = "Jane Doer" }).CheckAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).First.ClickAsync();
            await Expect(Page).ToHaveURLAsync(new Regex("/$"));

            await Page.GotoAsync($"{BaseUrl}/events/{id}");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Buy ticket" }).ClickAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Place order" }).ClickAsync();
            await Expect(Page).ToHaveURLAsync(new Regex("/orders/\\d+$"));

            await Page.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Link, new() { Name = "My orders" }).ClickAsync();
            var order = Page.GetByRole(AriaRole.List, new() { Name = "orders" }).GetByRole(AriaRole.Listitem).Filter(new() { HasText = title });
            await Expect(order).ToContainTextAsync("$7.00");
        }
        finally
        {
            await Page.GetByRole(AriaRole.Button, new() { Name = "Sign out" }).ClickAsync();
            await SignInAsync(1);
            await Page.APIRequest.DeleteAsync($"{BaseUrl}/api/v1/events/{id}");
        }
    }
}
