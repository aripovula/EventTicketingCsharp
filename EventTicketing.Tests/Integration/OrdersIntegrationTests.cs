using System.Net;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class OrdersIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Get_WithNonNumericId_Returns404()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/orders/abc", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
