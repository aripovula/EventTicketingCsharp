using System.Net;
using Microsoft.Extensions.Options;

namespace EventTicketing.DemoLauncher.Tests;

public class HttpDemoHealthTests
{
    private static readonly IOptions<DemoOptions> Options =
        Microsoft.Extensions.Options.Options.Create(new DemoOptions { DnsLabel = "eventticketing-demo", Location = "eastus" });

    private class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public Uri? Requested { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requested = request.RequestUri;
            return Task.FromResult(respond(request));
        }
    }

    private static Task<bool> IsUpAsync(StubHandler handler) =>
        new HttpDemoHealth(new HttpClient(handler), Options).IsUpAsync(TestContext.Current.CancellationToken);

    [Fact]
    public async Task AReadyDemo_IsUp()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        Assert.True(await IsUpAsync(handler));
        Assert.Equal("https://eventticketing-demo.eastus.cloudapp.azure.com/health/ready", handler.Requested?.ToString());
    }

    [Fact]
    public async Task ADemoStillStartingItsServices_IsNotUp()
    {
        Assert.False(await IsUpAsync(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))));
    }

    [Fact]
    public async Task ADemoThatDoesNotAnswerYet_IsNotUp()
    {
        Assert.False(await IsUpAsync(new StubHandler(_ => throw new HttpRequestException("Connection refused"))));
    }
}
