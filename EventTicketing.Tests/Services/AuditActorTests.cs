using System.Security.Claims;
using EventTicketing.Api.Services;

namespace EventTicketing.Tests.Services;

public class AuditActorTests
{
    [Fact]
    public void FromClaims_ForASignedInUser_ReadsTheIdAndEmail()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "5"), new Claim(ClaimTypes.Email, "john@example.com")], "test"));

        Assert.Equal(new AuditActor(5, "john@example.com"), AuditActor.FromClaims(user));
    }

    [Fact]
    public void FromClaims_ForAnAnonymousCaller_IsEmpty()
    {
        Assert.Equal(new AuditActor(null, null), AuditActor.FromClaims(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
