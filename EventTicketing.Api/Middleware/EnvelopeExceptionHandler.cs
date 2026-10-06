using EventTicketing.Api.Contracts;
using Microsoft.AspNetCore.Diagnostics;

namespace EventTicketing.Api.Middleware;

public class EnvelopeExceptionHandler(ILogger<EnvelopeExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var error = new ApiError("internal_error", "An unexpected error occurred.");
        await context.Response.WriteAsJsonAsync(new ApiEnvelope(null, [error]), cancellationToken);

        return true;
    }
}
