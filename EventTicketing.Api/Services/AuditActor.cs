using System.Security.Claims;

namespace EventTicketing.Api.Services;

/// <summary>Who performed an audited action; both parts are null for an anonymous caller.</summary>
public record AuditActor(int? UserId, string? Email)
{
    public static AuditActor FromClaims(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return new AuditActor(id is null ? null : int.Parse(id), user.FindFirstValue(ClaimTypes.Email));
    }
}
