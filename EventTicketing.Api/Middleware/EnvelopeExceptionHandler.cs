using Microsoft.AspNetCore.Diagnostics;

namespace EventTicketing.Api.Middleware;

public class EnvelopeExceptionHandler : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(false);
    }
}
