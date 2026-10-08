using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.EntityFrameworkCore;
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

    public async Task<RefreshTokenRotation?> RotateAsync(string rawToken, CancellationToken cancellationToken)
    {
        var tokenHash = Hash(rawToken);
        var token = await db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token is null || token.RevokedAt is not null || token.ExpiresAt <= DateTime.UtcNow)
            return null;

        var user = token.User;
        return new RefreshTokenRotation(new UserInfo(user.Id, user.Name, user.Email, user.Role), rawToken);
    }

    public static string Hash(string rawToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
