using System.Globalization;
using EventTicketing.Api.Contracts;
using EventTicketing.Api.Payments;
using Microsoft.AspNetCore.Diagnostics;

namespace EventTicketing.Api.Middleware;

/// <summary>Answers 503 with Retry-After while Stripe's circuit breaker is open, from any endpoint that takes payments.</summary>
public class PaymentsUnavailableHandler(ILogger<PaymentsUnavailableHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not PaymentsUnavailableException unavailable)
            return false;

        logger.LogWarning("Payments are unavailable; Stripe's circuit breaker is open");
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = Math.Ceiling(unavailable.RetryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        var error = new ApiError("payments_unavailable", "Payments are temporarily unavailable. Please try again shortly.");
        await context.Response.WriteAsJsonAsync(new ApiEnvelope(null, [error]), cancellationToken);
        return true;
    }
}
