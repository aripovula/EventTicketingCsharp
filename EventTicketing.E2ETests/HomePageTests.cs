using Microsoft.Playwright.NUnit;

namespace EventTicketing.E2ETests;

public class HomePageTests : PageTest
{
    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5173";

    [Test]
    public async Task HomepageLoads()
    {
        await Page.GotoAsync(BaseUrl);

        await Expect(Page).ToHaveTitleAsync("Event Ticketing");
    }
}
