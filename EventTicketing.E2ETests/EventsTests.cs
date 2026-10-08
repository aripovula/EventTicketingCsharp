using Microsoft.Playwright;

namespace EventTicketing.E2ETests;

public class EventsTests : E2ETestBase
{
    [Test]
    public async Task HomePageListsSeededEvents()
    {
        await Page.GotoAsync(BaseUrl);

        var list = Page.GetByRole(AriaRole.List, new() { Name = "events" });
        var jazz = list.GetByRole(AriaRole.Listitem).Filter(new() { HasText = "Jazz Night" });
        await Expect(jazz).ToContainTextAsync("Blue Note Club");
        await Expect(jazz).ToContainTextAsync("$25.00");
    }
}
