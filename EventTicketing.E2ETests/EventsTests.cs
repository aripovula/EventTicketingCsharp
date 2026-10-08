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

    [Test]
    public async Task SortByPriceShowsTheCheapestEventFirst()
    {
        var cheapest = await Page.APIRequest.GetAsync($"{BaseUrl}/api/v1/events?sort=price&limit=1");
        var cheapestTitle = (await cheapest.JsonAsync())!.Value.GetProperty("data")[0].GetProperty("title").GetString()!;
        await Page.GotoAsync(BaseUrl);
        var items = Page.GetByRole(AriaRole.List, new() { Name = "events" }).GetByRole(AriaRole.Listitem);
        await Expect(items.First).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Combobox, new() { Name = "Sort events" }).SelectOptionAsync("price");

        await Expect(items.First.GetByRole(AriaRole.Heading)).ToHaveTextAsync(cheapestTitle);
    }

    [Test]
    public async Task ScrollingToTheBottomLoadsAllSeededEvents()
    {
        await Page.GotoAsync(BaseUrl);
        var items = Page.GetByRole(AriaRole.List, new() { Name = "events" }).GetByRole(AriaRole.Listitem);
        await Expect(items).ToHaveCountAsync(20);

        await Page.GetByTestId("events-sentinel").ScrollIntoViewIfNeededAsync();

        await Expect(items).ToHaveCountAsync(30);
    }

    [Test]
    public async Task ViewingAnEventShowsItsDetails()
    {
        await Page.GotoAsync(BaseUrl);

        await Page.GetByRole(AriaRole.Link, new() { Name = "View Jazz Night" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Jazz Night" })).ToBeVisibleAsync();
        await Expect(Page.GetByText("48 of 120 seats available")).ToBeVisibleAsync();
        await Expect(Page.GetByText("$25.00")).ToBeVisibleAsync();
    }

    [Test]
    public async Task UnknownEventShowsNotFound()
    {
        await Page.GotoAsync($"{BaseUrl}/events/999999");

        await Expect(Page.GetByText("Event not found.")).ToBeVisibleAsync();
    }
}
