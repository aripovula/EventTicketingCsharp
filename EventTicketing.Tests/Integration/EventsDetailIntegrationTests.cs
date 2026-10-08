using System.Net;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class EventsDetailIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_WithNonNumericId_Returns404()
    {
        var response = await _client.GetAsync("/api/v1/events/jazz", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
