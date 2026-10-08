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

    private static string[] Titles(JsonElement body) =>
        body.GetProperty("data").EnumerateArray().Select(e => e.GetProperty("title").GetString()!).ToArray();

    [Fact]
    public async Task List_ReturnsTwentyEventsSortedByTitleByDefault()
    {
        var (_, body) = await GetAsync("/api/v1/events");

        var titles = Titles(body);
        Assert.Equal(20, titles.Length);
        Assert.Equal(titles.Order(StringComparer.Ordinal), titles);
    }

    [Fact]
    public async Task List_HonoursTheLimitParameter()
    {
        var (_, body) = await GetAsync("/api/v1/events?limit=5");

        Assert.Equal(5, Titles(body).Length);
    }

    [Fact]
    public async Task List_ReturnsTheEventFields()
    {
        var (_, body) = await GetAsync("/api/v1/events?limit=50");

        var jazz = body.GetProperty("data").EnumerateArray().Single(e => e.GetProperty("title").GetString() == "Jazz Night");
        Assert.Equal("Blue Note Club", jazz.GetProperty("venue").GetString());
        Assert.Equal("Music", jazz.GetProperty("eventType").GetString());
        Assert.Equal(2500, jazz.GetProperty("priceCents").GetInt32());
        Assert.Equal(120, jazz.GetProperty("totalSeats").GetInt32());
        Assert.Equal(48, jazz.GetProperty("availableSeats").GetInt32());
    }

    private async Task<List<string>> AllTitlesAsync(string query)
    {
        var titles = new List<string>();
        string? cursor = null;
        do
        {
            var url = $"/api/v1/events?{query}" + (cursor is null ? "" : $"&after={Uri.EscapeDataString(cursor)}");
            var (status, body) = await GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, status);
            titles.AddRange(Titles(body));
            var next = body.GetProperty("meta").GetProperty("nextCursor");
            cursor = next.ValueKind == JsonValueKind.Null ? null : next.GetString();
        } while (cursor is not null);
        return titles;
    }

    [Fact]
    public async Task List_PagingWithTheCursorWalksAllEventsInOrderWithoutRepeats()
    {
        var titles = await AllTitlesAsync("limit=7");

        Assert.Equal(30, titles.Count);
        Assert.Equal(titles.Order(StringComparer.Ordinal), titles);
    }

    [Fact]
    public async Task List_LastPageHasNoNextCursor()
    {
        var (_, body) = await GetAsync("/api/v1/events?limit=50");

        Assert.Equal(JsonValueKind.Null, body.GetProperty("meta").GetProperty("nextCursor").ValueKind);
    }
}
