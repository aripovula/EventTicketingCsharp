using System.Net;
using System.Net.Http.Json;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class AuthRefreshIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Refresh_WithoutRefreshCookie_Returns401()
    {
        var response = await factory.CreateClient().PostAsync("/api/v1/auth/refresh", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<(HttpClient Client, string RefreshToken)> LoginAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "john@example.com", password = "Password" },
            TestContext.Current.CancellationToken);
        return (client, CookieValue(login, "refresh_token"));
    }

    private static string CookieValue(HttpResponseMessage response, string name) =>
        response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith($"{name}=")).Split(';')[0][(name.Length + 1)..];

    [Fact]
    public async Task Refresh_AfterLogin_Returns200WithNewAccessAndRefreshCookies()
    {
        var (client, loginRefreshToken) = await LoginAsync();

        var response = await client.PostAsync("/api/v1/auth/refresh", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(CookieValue(response, "access_token"));
        Assert.NotEqual(loginRefreshToken, CookieValue(response, "refresh_token"));
    }

    [Fact]
    public async Task Refresh_CanBeRepeatedWithTheRotatedCookie()
    {
        var (client, _) = await LoginAsync();

        await client.PostAsync("/api/v1/auth/refresh", null, TestContext.Current.CancellationToken);
        var second = await client.PostAsync("/api/v1/auth/refresh", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Refresh_ReplayingAnOldRefreshToken_Returns401()
    {
        var (client, loginRefreshToken) = await LoginAsync();
        await client.PostAsync("/api/v1/auth/refresh", null, TestContext.Current.CancellationToken);

        var attacker = factory.CreateClient(new() { HandleCookies = false });
        using var replay = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        replay.Headers.Add("Cookie", $"refresh_token={loginRefreshToken}");
        var response = await attacker.SendAsync(replay, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
