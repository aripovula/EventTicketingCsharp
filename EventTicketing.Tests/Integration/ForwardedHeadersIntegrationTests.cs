using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class ForwardedHeadersIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    // TestServer requests have no remote address; this gives them one, as a real connection would.
    private sealed class RemoteAddress(IPAddress address) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = address;
                return nextMiddleware(context);
            });
            next(app);
        };
    }

    // A global limit of 2 requests a minute, with every request arriving from the given address.
    private HttpClient ClientFrom(IPAddress address) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:GlobalPermitLimit", "2");
            builder.ConfigureTestServices(services => services.AddSingleton<IStartupFilter>(new RemoteAddress(address)));
        }).CreateClient();

    private static Task<HttpResponseMessage> GetAsForwardedFor(HttpClient client, string clientAddress)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/events/1");
        request.Headers.Add("X-Forwarded-For", clientAddress);
        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BehindALocalProxy_EachClientGetsItsOwnLimit()
    {
        var client = ClientFrom(IPAddress.Loopback);
        await GetAsForwardedFor(client, "198.51.100.1");
        await GetAsForwardedFor(client, "198.51.100.1");

        var otherClient = await GetAsForwardedFor(client, "198.51.100.2");

        Assert.Equal(HttpStatusCode.OK, otherClient.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await GetAsForwardedFor(client, "198.51.100.1")).StatusCode);
    }

    [Fact]
    public async Task FromAnythingButALocalProxy_TheHeaderIsIgnored()
    {
        var client = ClientFrom(IPAddress.Parse("203.0.113.9"));
        await GetAsForwardedFor(client, "198.51.100.1");
        await GetAsForwardedFor(client, "198.51.100.2");

        // A different claimed address doesn't buy a fresh allowance.
        Assert.Equal(HttpStatusCode.TooManyRequests, (await GetAsForwardedFor(client, "198.51.100.3")).StatusCode);
    }
}
