using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using OpenTelemetry.Trace;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class TracingIntegrationTests(TracingIntegrationTests.HostWithSpans factory) : IClassFixture<TracingIntegrationTests.HostWithSpans>
{
    /// <summary>
    /// Keeps every finished span in memory. Spans from other test hosts in this process land here
    /// too (activity sources are process-wide), so each test picks out its own trace.
    /// </summary>
    public class HostWithSpans : IntegrationTestFactory
    {
        public List<Activity> Spans { get; } = [];

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
                services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddInMemoryExporter(Spans)));
        }

        public List<Activity> SpansOf(ActivityTraceId traceId)
        {
            lock (Spans)
                return Spans.Where(s => s.TraceId == traceId).ToList();
        }
    }

    private static (ActivityTraceId TraceId, HttpRequestMessage Request) TracedRequest(string path)
    {
        var traceId = ActivityTraceId.CreateRandom();
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("traceparent", $"00-{traceId}-{ActivitySpanId.CreateRandom()}-01");
        return (traceId, request);
    }

    // The server span is exported as the request finishes, which can be just after the client has the response.
    private async Task<List<Activity>> EventuallySpansOfAsync(ActivityTraceId traceId, Func<List<Activity>, bool> done)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!done(factory.SpansOf(traceId)))
        {
            Assert.True(DateTime.UtcNow < deadline, "Spans not exported within 5 seconds");
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
        return factory.SpansOf(traceId);
    }

    [Fact]
    public async Task Request_IsTracedWithItsSqlInTheCallersTrace()
    {
        var (traceId, request) = TracedRequest("/api/v1/events/1");

        await factory.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        var spans = await EventuallySpansOfAsync(traceId, s => s.Any(a => a.Kind == ActivityKind.Server));
        Assert.Contains(spans, s => s.Kind == ActivityKind.Server && s.DisplayName == "GET api/v1/events/{id:int}");
        Assert.Contains(spans, s => s.Source.Name == "Npgsql");
    }

    [Fact]
    public async Task HealthChecks_AreNotTraced()
    {
        var (traceId, request) = TracedRequest("/health");

        await factory.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);
        await Task.Delay(500, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(factory.SpansOf(traceId), s => s.Kind == ActivityKind.Server);
    }
}
