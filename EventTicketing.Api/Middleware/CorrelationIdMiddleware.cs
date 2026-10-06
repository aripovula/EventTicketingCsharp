namespace EventTicketing.Api.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();

        if (correlationId is not null)
            context.Response.Headers[HeaderName] = correlationId;

        await next(context);
    }
}
