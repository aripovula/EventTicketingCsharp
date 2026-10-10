using System.Net;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class TicketingHubIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Negotiate_AnonymousClient_OffersWebSockets()
    {
        var response = await factory.CreateClient().PostAsync("/hubs/ticketing/negotiate?negotiateVersion=1", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("WebSockets", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
