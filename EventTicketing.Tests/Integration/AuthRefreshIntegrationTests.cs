using System.Net;

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
}
