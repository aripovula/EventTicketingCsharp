using System.Net;
using System.Text.Json;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class SwaggerIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SwaggerJson_DescribesEventTicketingApi()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var spec = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Event Ticketing API", spec.RootElement.GetProperty("info").GetProperty("title").GetString());
    }

    [Fact]
    public async Task SwaggerUi_IsServed()
    {
        var response = await _client.GetAsync("/swagger/index.html", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerJson_IncludesXmlDocSummaries()
    {
        var json = await _client.GetStringAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        var login = JsonDocument.Parse(json).RootElement.GetProperty("paths").GetProperty("/api/v1/auth/login").GetProperty("post");
        Assert.StartsWith("Signs in with email and password", login.GetProperty("summary").GetString());
    }

    [Fact]
    public async Task SwaggerJson_DeclaresJwtBearerSecurity()
    {
        var json = await _client.GetStringAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        var root = JsonDocument.Parse(json).RootElement;
        var bearer = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.True(root.GetProperty("security")[0].TryGetProperty("Bearer", out _));
    }
}
