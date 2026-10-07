namespace EventTicketing.E2ETests;

public class HomePageTests : E2ETestBase
{
    [Test]
    public async Task HomepageLoads()
    {
        await Page.GotoAsync(BaseUrl);

        await Expect(Page).ToHaveTitleAsync("Event Ticketing");
    }
}
