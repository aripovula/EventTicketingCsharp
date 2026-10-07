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
    IOptions<JwtOptions> jwtOptions,
    IWebHostEnvironment env) : ControllerBase
{
    public const string AccessTokenCookie = "access_token";

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await authService.LoginAsync(request, cancellationToken);
        if (user is null)
            return Unauthorized(new ApiEnvelope(null, [new ApiError("invalid_credentials", "Invalid email or password.")]));

        Response.Cookies.Append(AccessTokenCookie, tokenService.GenerateAccessToken(user), new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.Value.AccessTokenMinutes),
        });

        return Ok(user);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AccessTokenCookie);
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
}
