using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class RateLimitingIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private HttpClient ClientWith(string setting, int limit) =>
        factory.WithWebHostBuilder(builder => builder.UseSetting(setting, limit.ToString())).CreateClient();

    [Fact]
    public async Task RequestsOverTheGlobalLimit_Return429()
    {
        var client = ClientWith("RateLimiting:GlobalPermitLimit", 3);
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/events/1", TestContext.Current.CancellationToken)).StatusCode);

        var response = await client.GetAsync("/api/v1/events/1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }
}
