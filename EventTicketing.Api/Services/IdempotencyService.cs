using EventTicketing.Api.Data;
using EventTicketing.Api.Models;

namespace EventTicketing.Api.Services;

/// <summary>Remembers responses by Idempotency-Key and request path for 24 hours.</summary>
public class IdempotencyService(AppDbContext db)
{
    public static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    /// <remarks>Throws DbUpdateException if the key was stored meanwhile by a concurrent request.</remarks>
    public async Task StoreAsync(string key, string requestPath, int statusCode, string responseBody, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        db.IdempotencyKeys.Add(new IdempotencyKey
        {
            Key = key,
            RequestPath = requestPath,
            StatusCode = statusCode,
            ResponseBody = responseBody,
            CreatedAt = now,
            ExpiresAt = now + Ttl,
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
