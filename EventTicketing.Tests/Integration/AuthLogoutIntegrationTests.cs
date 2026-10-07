using System.Net;
using System.Net.Http.Json;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class AuthLogoutIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Logout_Returns204AndExpiresAccessTokenCookie()
    {
        var response = await factory.CreateClient().PostAsync("/api/v1/auth/logout", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("access_token="));
        Assert.Contains("expires=Thu, 01 Jan 1970", cookie);
    }

    [Fact]
    public async Task Me_AfterLogout_Returns401()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "john@example.com", password = "Password" },
            TestContext.Current.CancellationToken);

        await client.PostAsync("/api/v1/auth/logout", null, TestContext.Current.CancellationToken);
        var response = await client.GetAsync("/api/v1/auth/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
