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
        var (_, rawToken) = AddToken(userId, familyId);
        await db.SaveChangesAsync(cancellationToken);

        return rawToken;
    }

    public async Task<RefreshTokenRotation?> RotateAsync(string rawToken, CancellationToken cancellationToken)
    {
        var tokenHash = Hash(rawToken);
        var token = await db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token is null || token.ExpiresAt <= DateTime.UtcNow)
            return null;

        // A revoked token being presented again means it was stolen or replayed:
        // revoke the whole family so neither the thief nor the victim can continue.
        if (token.RevokedAt is not null)
        {
            await db.RefreshTokens
                .Where(t => t.FamilyId == token.FamilyId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), cancellationToken);
            return null;
        }

        token.RevokedAt = DateTime.UtcNow;
        var (next, nextRawToken) = AddToken(token.UserId, token.FamilyId);
        await db.SaveChangesAsync(cancellationToken);
        token.ReplacedById = next.Id;
        await db.SaveChangesAsync(cancellationToken);

        var user = token.User;
        return new RefreshTokenRotation(new UserInfo(user.Id, user.Name, user.Email, user.Role), nextRawToken);
    }

    private (RefreshToken Token, string RawToken) AddToken(int userId, Guid familyId)
    {
        var rawToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));
        var now = DateTime.UtcNow;
        var token = new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(rawToken),
            FamilyId = familyId,
            CreatedAt = now,
            ExpiresAt = now.AddDays(options.Value.RefreshTokenDays),
        };

        db.RefreshTokens.Add(token);
        return (token, rawToken);
    }

    public static string Hash(string rawToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
