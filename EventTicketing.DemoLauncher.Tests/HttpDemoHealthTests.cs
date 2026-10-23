using System.Net;

namespace EventTicketing.DemoLauncher.Tests;

public class HttpDemoHealthTests
{
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
        new HttpDemoHealth(new HttpClient(handler))
            .IsUpAsync("https://eventticketing-demo.eastus.cloudapp.azure.com", TestContext.Current.CancellationToken);

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
