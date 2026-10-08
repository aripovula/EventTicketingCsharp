using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace EventTicketing.E2ETests;

public class RegisterTests : E2ETestBase
{
    [Test]
    public async Task NewAccountCanRegisterAndThenSignIn()
    {
        var email = $"e2e.{Guid.NewGuid():N}@example.com";

        await Page.GotoAsync($"{BaseUrl}/register");
        await Page.GetByLabel("Name").FillAsync("E2E User");
        await Page.GetByLabel("Email").FillAsync(email);
        await Page.GetByLabel("Password").FillAsync("e2e-password");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/login$"));

        var panel = Page.Locator("div", new() { Has = Page.GetByRole(AriaRole.Heading, new() { Name = "Sign in with your account" }) }).Last;
        await panel.GetByLabel("Email").FillAsync(email);
        await panel.GetByLabel("Password").FillAsync("e2e-password");
        await panel.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
    }

    [Test]
    public async Task RegisteringATakenEmailShowsTheErrorUnderTheEmailField()
    {
        await Page.GotoAsync($"{BaseUrl}/register");
        await Page.GetByLabel("Name").FillAsync("Another John");
        await Page.GetByLabel("Email").FillAsync("john@example.com");
        await Page.GetByLabel("Password").FillAsync("e2e-password");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();

        await Expect(Page.GetByText("An account with this email already exists.")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Email")).ToHaveAttributeAsync("aria-invalid", "true");
    }
}
