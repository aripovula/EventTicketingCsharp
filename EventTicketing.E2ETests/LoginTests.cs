using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace EventTicketing.E2ETests;

public class LoginTests : E2ETestBase
{
    [Test]
    public async Task DemoUserSignInSetsHttpOnlyCookieAndGoesHome()
    {
        await Page.GotoAsync($"{BaseUrl}/login");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).First.ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
        var cookie = (await Context.CookiesAsync()).Single(c => c.Name == "access_token");
        Assert.That(cookie.HttpOnly, Is.True);
    }

    [Test]
    public async Task AccountFormSignInWithSeededCredentialsGoesHome()
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        var panel = Page.Locator("div", new() { Has = Page.GetByRole(AriaRole.Heading, new() { Name = "Sign in with your account" }) }).Last;

        await panel.GetByLabel("Email").FillAsync("jane@example.com");
        await panel.GetByLabel("Password").FillAsync("Password");
        await panel.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
    }

    [Test]
    public async Task AccountFormSignInWithWrongPasswordShowsError()
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        var panel = Page.Locator("div", new() { Has = Page.GetByRole(AriaRole.Heading, new() { Name = "Sign in with your account" }) }).Last;

        await panel.GetByLabel("Email").FillAsync("jane@example.com");
        await panel.GetByLabel("Password").FillAsync("wrong-password");
        await panel.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await Expect(panel.GetByRole(AriaRole.Alert)).ToHaveTextAsync("Invalid email or password.");
    }

    [Test]
    public async Task SignOutFromHeaderClearsCookieAndShowsSignIn()
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).First.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));

        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign out" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/login$"));
        await Expect(Page.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Link, new() { Name = "Sign in" })).ToBeVisibleAsync();
        Assert.That((await Context.CookiesAsync()).Any(c => c.Name == "access_token"), Is.False);
    }
}
