using EventTicketing.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace EventTicketing.Tests.Middleware;

public class CorrelationIdMiddlewareTests
{
    private const string HeaderName = "X-Correlation-ID";

    private static CorrelationIdMiddleware Build(RequestDelegate? next = null)
    {
        next ??= _ => Task.CompletedTask;
        return new CorrelationIdMiddleware(next);
    }

    [Fact]
    public async Task EchosCallerSuppliedCorrelationId()
    {
        var middleware = Build();
        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderName] = "my-trace-id";

        await middleware.InvokeAsync(context);

        Assert.Equal("my-trace-id", context.Response.Headers[HeaderName].ToString());
    }

    [Fact]
    public async Task CallsNextMiddleware()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }
}
