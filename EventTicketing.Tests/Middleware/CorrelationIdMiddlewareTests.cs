using EventTicketing.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace EventTicketing.Tests.Middleware;

public class CorrelationIdMiddlewareTests
{
    private static CorrelationIdMiddleware Build(RequestDelegate? next = null)
    {
        next ??= _ => Task.CompletedTask;
        return new CorrelationIdMiddleware(next);
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
