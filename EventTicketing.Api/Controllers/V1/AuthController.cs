using System.Security.Claims;
using EventTicketing.Api.Contracts;
using EventTicketing.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EventTicketing.Api.Controllers.V1;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    AuthService authService,
    TokenService tokenService,
    RefreshTokenService refreshTokens,
    IOptions<JwtOptions> jwtOptions,
    IWebHostEnvironment env) : ControllerBase
{
    public const string AccessTokenCookie = "access_token";
    public const string RefreshTokenCookie = "refresh_token";
    public const string RefreshTokenPath = "/api/v1/auth";

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await authService.LoginAsync(request, cancellationToken);
        if (user is null)
            return Unauthorized(new ApiEnvelope(null, [new ApiError("invalid_credentials", "Invalid email or password.")]));

        AppendAccessTokenCookie(user);
        AppendRefreshTokenCookie(await refreshTokens.IssueAsync(user.UserId, Guid.NewGuid(), cancellationToken));

        return Ok(user);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = await authService.RegisterAsync(request, cancellationToken);
        if (user is null)
            return Conflict(new ApiEnvelope(null, [new ApiError("email_taken", "An account with this email already exists.", "email")]));

        return StatusCode(StatusCodes.Status201Created, user);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var rawToken = Request.Cookies[RefreshTokenCookie];
        var rotation = rawToken is null ? null : await refreshTokens.RotateAsync(rawToken, cancellationToken);
        if (rotation is null)
            return Unauthorized();

        AppendAccessTokenCookie(rotation.User);
        AppendRefreshTokenCookie(rotation.Token);
        return Ok(rotation.User);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var rawToken = Request.Cookies[RefreshTokenCookie];
        if (rawToken is not null)
            await refreshTokens.RevokeAsync(rawToken, cancellationToken);

        Response.Cookies.Delete(AccessTokenCookie);
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions { Path = RefreshTokenPath });
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue(ClaimTypes.Name);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (id is null || name is null || email is null || role is null)
            return Unauthorized();

        return Ok(new UserInfo(int.Parse(id), name, email, role));
    }

    private void AppendAccessTokenCookie(UserInfo user)
    {
        Response.Cookies.Append(AccessTokenCookie, tokenService.GenerateAccessToken(user), new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.Value.AccessTokenMinutes),
        });
    }

    private void AppendRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append(RefreshTokenCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = RefreshTokenPath,
            Expires = DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays),
        });
    }
}
