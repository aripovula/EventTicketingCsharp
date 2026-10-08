using System.Net;
using System.Text.Json;

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

    [Fact]
    public async Task Get_ReturnsTheEvent()
    {
        var list = JsonDocument.Parse(await _client.GetStringAsync("/api/v1/events?q=Jazz%20Night", TestContext.Current.CancellationToken));
        var id = list.RootElement.GetProperty("data")[0].GetProperty("id").GetInt32();

        var response = await _client.GetAsync($"/api/v1/events/{id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.GetProperty("data");
        Assert.Equal(id, data.GetProperty("id").GetInt32());
        Assert.Equal("Jazz Night", data.GetProperty("title").GetString());
        Assert.StartsWith("An intimate evening of live jazz", data.GetProperty("description").GetString());
    }
}
