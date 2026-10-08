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

    [Test]
    public async Task SearchShowsOnlyMatchingEvents()
    {
        await Page.GotoAsync(BaseUrl);
        var items = Page.GetByRole(AriaRole.List, new() { Name = "events" }).GetByRole(AriaRole.Listitem);
        await Expect(items.First).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Search events" }).FillAsync("jazz");

        await Expect(items.Filter(new() { HasNotText = "Jazz" })).ToHaveCountAsync(0);
        await Expect(items.Filter(new() { HasText = "Jazz Night" })).ToHaveCountAsync(1);
    }
}
