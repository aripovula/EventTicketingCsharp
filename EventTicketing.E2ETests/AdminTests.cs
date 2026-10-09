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
}
