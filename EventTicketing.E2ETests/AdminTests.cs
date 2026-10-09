using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace EventTicketing.E2ETests;

public class AdminTests : E2ETestBase
{
    private async Task SignInAsAdminAsync()
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).Nth(1).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
    }

    [Test]
    public async Task AdminOpensTheAdminPageFromTheHeader()
    {
        await SignInAsAdminAsync();

        await Page.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Link, new() { Name = "Admin" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Admin" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AdminPageListsEventsWithEditLinks()
    {
        await SignInAsAdminAsync();
        await Page.GotoAsync($"{BaseUrl}/admin");

        var list = Page.GetByRole(AriaRole.List, new() { Name = "admin events" });
        await Expect(list.GetByRole(AriaRole.Listitem).Filter(new() { HasText = "Jazz Night" })).ToContainTextAsync("Blue Note Club");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Edit Jazz Night" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AdminDeletesAnEventAfterConfirming()
    {
        await SignInAsAdminAsync();
        var title = $"E2E Delete {Guid.NewGuid():N}"[..20];
        var created = await Page.APIRequest.PostAsync($"{BaseUrl}/api/v1/events", new()
        {
            DataObject = new
            {
                title,
                description = "Created by an E2E test.",
                startTime = DateTime.UtcNow.AddDays(30),
                endTime = DateTime.UtcNow.AddDays(30).AddHours(2),
                venue = "Test Hall",
                eventType = "Other",
                totalSeats = 10,
                availableSeats = 10,
                priceCents = 100,
            },
        });
        Assert.That(created.Status, Is.EqualTo(201));
        await Page.GotoAsync($"{BaseUrl}/admin");

        await Page.GetByRole(AriaRole.Button, new() { Name = $"Delete {title}" }).ClickAsync();
        await Page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.List, new() { Name = "admin events" }).GetByText(title)).ToHaveCountAsync(0);
        var gone = await Page.APIRequest.GetAsync($"{BaseUrl}/api/v1/events?q={Uri.EscapeDataString(title)}");
        Assert.That((await gone.TextAsync()).Contains(title), Is.False);
    }
}
