using System.Globalization;
using System.Threading.RateLimiting;
using EventTicketing.Api.Contracts;
using Microsoft.AspNetCore.RateLimiting;

namespace EventTicketing.Api.Middleware;

/// <summary>Turns a rate-limiter rejection into the usual error envelope, plus Retry-After when known.</summary>
public static class RateLimitRejection
{
    public static async ValueTask WriteEnvelopeAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);

        var error = new ApiError("rate_limited", "Too many requests. Please wait a moment and try again.");
        await context.HttpContext.Response.WriteAsJsonAsync(new ApiEnvelope(null, [error]), cancellationToken);
    }
}
