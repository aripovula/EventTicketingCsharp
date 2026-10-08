using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.Extensions.Options;

namespace EventTicketing.Api.Services;

public class RefreshTokenService(AppDbContext db, IOptions<JwtOptions> options)
{
    public async Task<string> IssueAsync(int userId, Guid familyId, CancellationToken cancellationToken)
    {
        var rawToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));
        var now = DateTime.UtcNow;

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(rawToken),
            FamilyId = familyId,
            CreatedAt = now,
            ExpiresAt = now.AddDays(options.Value.RefreshTokenDays),
        });
        await db.SaveChangesAsync(cancellationToken);

        return rawToken;
    }

    public Task<RefreshTokenRotation?> RotateAsync(string rawToken, CancellationToken cancellationToken)
    {
        return Task.FromResult<RefreshTokenRotation?>(null);
    }

    public static string Hash(string rawToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
