using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class AuthMeIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Me_WithoutAuthCookie_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/auth/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_AfterLogin_ReturnsSignedInUser()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "jane@example.com", password = "Password" },
            TestContext.Current.CancellationToken);

        var response = await client.GetAsync("/api/v1/auth/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.GetProperty("data");
        Assert.Equal("Jane Doer", data.GetProperty("name").GetString());
        Assert.Equal("jane@example.com", data.GetProperty("email").GetString());
        Assert.Equal("user", data.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Me_WithBearerHeader_ReturnsSignedInUser()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "admin@example.com", password = "Password" },
            TestContext.Current.CancellationToken);
        var token = login.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("access_token="))
            .Split(';')[0]["access_token=".Length..];
        var bearerClient = factory.CreateClient(new() { HandleCookies = false });
        bearerClient.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await bearerClient.GetAsync("/api/v1/auth/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
