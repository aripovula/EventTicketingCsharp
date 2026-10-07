using System.Security.Claims;
using System.Text;
using EventTicketing.Api.Contracts;
using EventTicketing.Api.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EventTicketing.Tests.Services;

public class TokenServiceTests
{
    private static readonly JwtOptions Jwt = new()
    {
        Issuer = "EventTicketing",
        Audience = "EventTicketingUsers",
        SigningKey = "test-signing-key-that-is-at-least-32-bytes",
        AccessTokenMinutes = 15,
    };

    private static readonly UserInfo John = new(7, "John Doe", "john@example.com", "user");

    private static string Generate() => new TokenService(Options.Create(Jwt)).GenerateAccessToken(John);

    private static Task<TokenValidationResult> Validate(string token, string signingKey) =>
        new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Jwt.Issuer,
            ValidAudience = Jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        });

    [Fact]
    public async Task TokenValidatesWithSigningKeyIssuerAndAudience()
    {
        var result = await Validate(Generate(), Jwt.SigningKey);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task TokenIsRejectedWithDifferentSigningKey()
    {
        var result = await Validate(Generate(), "another-signing-key-that-is-32-bytes-long");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task TokenCarriesUserIdEmailNameAndRoleClaims()
    {
        var identity = (await Validate(Generate(), Jwt.SigningKey)).ClaimsIdentity;

        Assert.Equal("7", identity.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("john@example.com", identity.FindFirst(ClaimTypes.Email)?.Value);
        Assert.Equal("John Doe", identity.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal("user", identity.FindFirst(ClaimTypes.Role)?.Value);
    }

    [Fact]
    public void TokenExpiresAfterConfiguredMinutes()
    {
        var token = new JsonWebToken(Generate());

        var expectedExpiry = DateTime.UtcNow.AddMinutes(Jwt.AccessTokenMinutes);
        Assert.InRange(token.ValidTo, expectedExpiry.AddMinutes(-1), expectedExpiry.AddMinutes(1));
    }
}
