using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Services;

/// <summary>Remembers responses by Idempotency-Key and request path for 24 hours.</summary>
public class IdempotencyService(AppDbContext db)
{
    public static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    /// <returns>The remembered response for this key and path, or null if there is none or it has expired.</returns>
    public async Task<IdempotencyKey?> GetAsync(string key, string requestPath, CancellationToken cancellationToken)
    {
        var entry = await db.IdempotencyKeys.AsNoTracking()
            .SingleOrDefaultAsync(k => k.Key == key && k.RequestPath == requestPath, cancellationToken);
        return entry is null || entry.ExpiresAt < DateTime.UtcNow ? null : entry;
    }

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
