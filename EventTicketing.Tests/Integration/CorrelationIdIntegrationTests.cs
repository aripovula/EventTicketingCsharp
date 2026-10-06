namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class CorrelationIdIntegrationTests : IClassFixture<IntegrationTestFactory>
{
    private const string HeaderName = "X-Correlation-ID";
    private readonly HttpClient _client;

    public CorrelationIdIntegrationTests(IntegrationTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Response_ContainsCorrelationIdHeader()
    {
        var response = await _client.GetAsync("/", TestContext.Current.CancellationToken);

        Assert.True(response.Headers.Contains(HeaderName));
    }

    [Fact]
    public async Task Response_EchosCallerSuppliedCorrelationId()
    {
        var requestId = "test-correlation-abc-123";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation(HeaderName, requestId);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(requestId, response.Headers.GetValues(HeaderName).First());
    }

    [Fact]
    public async Task Response_GeneratesCorrelationIdWhenNoneSupplied()
    {
        var response = await _client.GetAsync("/", TestContext.Current.CancellationToken);

        var id = response.Headers.GetValues(HeaderName).First();
        Assert.True(Guid.TryParse(id, out _));
    }
}
