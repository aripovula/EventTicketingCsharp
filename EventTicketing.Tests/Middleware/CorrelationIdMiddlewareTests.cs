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
    public async Task GeneratesCorrelationIdWhenHeaderAbsent()
    {
        var middleware = Build();
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.False(string.IsNullOrEmpty(context.Response.Headers[HeaderName].ToString()));
    }

    [Fact]
    public async Task GeneratedCorrelationIdIsValidGuid()
    {
        var middleware = Build();
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        var id = context.Response.Headers[HeaderName].ToString();
        Assert.True(Guid.TryParse(id, out _));
    }

    [Fact]
    public async Task EachRequestGetsUniqueGeneratedId()
    {
        var middleware = Build();

        var ctx1 = new DefaultHttpContext();
        var ctx2 = new DefaultHttpContext();

        await middleware.InvokeAsync(ctx1);
        await middleware.InvokeAsync(ctx2);

        var id1 = ctx1.Response.Headers[HeaderName].ToString();
        var id2 = ctx2.Response.Headers[HeaderName].ToString();

        Assert.NotEqual(id1, id2);
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
