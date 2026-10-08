using System.Net;
using System.Text.Json;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class EventsListIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<(HttpStatusCode Status, JsonElement Body)> GetAsync(string url)
    {
        var response = await _client.GetAsync(url, TestContext.Current.CancellationToken);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
        return (response.StatusCode, body);
    }

    [Fact]
    public async Task List_ReturnsAnEnvelopeWithADataArrayAndCursorMeta()
    {
        var (status, body) = await GetAsync("/api/v1/events");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(JsonValueKind.Array, body.GetProperty("data").ValueKind);
        Assert.True(body.GetProperty("meta").TryGetProperty("nextCursor", out _));
    }

    [Theory]
    [InlineData("/api/v1/events?sort=random", "sort")]
    [InlineData("/api/v1/events?limit=0", "limit")]
    [InlineData("/api/v1/events?limit=51", "limit")]
    public async Task List_WithInvalidQuery_Returns400ForThatParameter(string url, string field)
    {
        var (status, body) = await GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Contains(body.GetProperty("errors").EnumerateArray(), e => e.GetProperty("field").GetString() == field);
    }
}
