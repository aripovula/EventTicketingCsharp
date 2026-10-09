using System.Net;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class AdminOrdersIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Orders_WithMalformedCursor_Returns400()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/admin/orders?after=garbage", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
