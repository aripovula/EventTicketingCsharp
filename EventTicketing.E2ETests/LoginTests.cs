using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace EventTicketing.E2ETests;

public class LoginTests : PageTest
{
    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5173";

    [Test]
    public async Task DemoUserSignInSetsHttpOnlyCookieAndGoesHome()
    {
        await Page.GotoAsync($"{BaseUrl}/login");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).First.ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
        var cookie = (await Context.CookiesAsync()).Single(c => c.Name == "access_token");
        Assert.That(cookie.HttpOnly, Is.True);
    }
}
