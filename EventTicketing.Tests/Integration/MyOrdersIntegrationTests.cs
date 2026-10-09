using System.Net;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class MyOrdersIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task MyOrders_WithoutSigningIn_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/auth/me/orders", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
