using System.Net;
using System.Net.Http.Json;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class AuthLoginIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_WithValidCredentials_Returns200()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "john@example.com", password = "Password" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMalformedEmail_Returns400ValidationEnvelope()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "not-an-email", password = "Password" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"field\":\"email\"", body);
    }

    [Theory]
    [InlineData("john@example.com", "wrong-password")]
    [InlineData("nobody@example.com", "Password")]
    public async Task Login_WithBadCredentials_Returns401InvalidCredentials(string email, string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email, password }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"code\":\"invalid_credentials\"", body);
    }

    [Fact]
    public async Task Login_WithValidCredentials_SetsHttpOnlyStrictAccessTokenCookie()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "john@example.com", password = "Password" },
            TestContext.Current.CancellationToken);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("access_token="));
        Assert.Contains("httponly", cookie);
        Assert.Contains("samesite=strict", cookie);
    }

    [Fact]
    public async Task Login_WithBadCredentials_DoesNotSetCookie()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "john@example.com", password = "wrong-password" },
            TestContext.Current.CancellationToken);

        Assert.False(response.Headers.Contains("Set-Cookie"));
    }
}
