using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class StaticUiIntegrationTests(StaticUiIntegrationTests.HostWithUi factory) : IClassFixture<StaticUiIntegrationTests.HostWithUi>
{
    /// <summary>A host whose web root holds a stand-in for the built React app, as a release build's wwwroot does.</summary>
    public sealed class HostWithUi : IntegrationTestFactory
    {
        public string WebRoot { get; } = Directory.CreateTempSubdirectory("event-ticketing-ui-").FullName;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            Directory.CreateDirectory(Path.Combine(WebRoot, "assets"));
            File.WriteAllText(Path.Combine(WebRoot, "index.html"), "<!doctype html><title>Event Ticketing</title>");
            File.WriteAllText(Path.Combine(WebRoot, "assets", "app.js"), "console.log('app')");
            builder.UseWebRoot(WebRoot);
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            Directory.Delete(WebRoot, recursive: true);
        }
    }

    private Task<HttpResponseMessage> GetAsync(string path) =>
        factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

    [Theory]
    [InlineData("/")]
    [InlineData("/events/5")]
    [InlineData("/admin/events/new")]
    public async Task AppRoutes_GetTheReactApp(string path)
    {
        var response = await GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<title>Event Ticketing</title>", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task BuiltAssets_AreServed()
    {
        var response = await GetAsync("/assets/app.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/javascript", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/api/v1/nothing-here")]
    [InlineData("/api")]
    [InlineData("/hubs/nothing-here")]
    public async Task UnknownApiPaths_StillGet404(string path)
    {
        var response = await GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
    }
}
