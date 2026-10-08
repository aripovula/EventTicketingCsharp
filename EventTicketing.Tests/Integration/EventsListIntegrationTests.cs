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

    [Theory]
    [InlineData("not-base64!!")]
    [InlineData("e30")]
    [InlineData("bm90LWpzb24")]
    public async Task List_WithMalformedCursor_Returns400InvalidCursor(string cursor)
    {
        var (status, body) = await GetAsync($"/api/v1/events?after={cursor}");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        var error = body.GetProperty("errors")[0];
        Assert.Equal("invalid_cursor", error.GetProperty("code").GetString());
        Assert.Equal("after", error.GetProperty("field").GetString());
    }

    [Fact]
    public async Task List_SearchMatchesTitlesCaseInsensitively()
    {
        var titles = await AllTitlesAsync("q=JAZZ");

        Assert.Contains("Jazz Night", titles);
        Assert.All(titles, t => Assert.Contains("jazz", t, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task List_SearchAndPagingWorkTogether()
    {
        var titles = await AllTitlesAsync("q=e&limit=3");

        Assert.Equal(titles.Distinct().Count(), titles.Count);
        Assert.All(titles, t => Assert.Contains("e", t, StringComparison.OrdinalIgnoreCase));
        Assert.True(titles.Count > 3);
    }

    [Theory]
    [InlineData("%25")]
    [InlineData("_")]
    public async Task List_SearchTreatsLikeWildcardsLiterally(string q)
    {
        var titles = await AllTitlesAsync($"q={q}");

        Assert.Empty(titles);
    }

    private async Task<List<JsonElement>> AllEventsAsync(string query)
    {
        var events = new List<JsonElement>();
        string? cursor = null;
        do
        {
            var url = $"/api/v1/events?{query}" + (cursor is null ? "" : $"&after={Uri.EscapeDataString(cursor)}");
            var (status, body) = await GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, status);
            events.AddRange(body.GetProperty("data").EnumerateArray().Select(e => e.Clone()));
            var next = body.GetProperty("meta").GetProperty("nextCursor");
            cursor = next.ValueKind == JsonValueKind.Null ? null : next.GetString();
        } while (cursor is not null);
        return events;
    }

    [Fact]
    public async Task List_SortByDatePagesThroughAllEventsInStartTimeOrder()
    {
        var events = await AllEventsAsync("sort=date&limit=4");

        var starts = events.Select(e => e.GetProperty("startTime").GetDateTime()).ToList();
        Assert.Equal(30, starts.Count);
        Assert.Equal(starts.Order(), starts);
    }

    [Fact]
    public async Task List_SortByPricePagesThroughAllEventsInPriceOrder()
    {
        var events = await AllEventsAsync("sort=price&limit=4");

        var prices = events.Select(e => e.GetProperty("priceCents").GetInt32()).ToList();
        Assert.Equal(30, prices.Count);
        Assert.Equal(prices.Order(), prices);
        Assert.Equal(30, events.Select(e => e.GetProperty("id").GetInt32()).Distinct().Count());
    }

    [Fact]
    public async Task List_CursorFromAnotherSortIsRejected()
    {
        var (_, first) = await GetAsync("/api/v1/events?sort=price&limit=5");
        var priceCursor = first.GetProperty("meta").GetProperty("nextCursor").GetString()!;

        var (status, _) = await GetAsync($"/api/v1/events?sort=date&after={Uri.EscapeDataString(priceCursor)}");

        Assert.Equal(HttpStatusCode.BadRequest, status);
    }
}
