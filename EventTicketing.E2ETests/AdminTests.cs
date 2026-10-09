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
}
