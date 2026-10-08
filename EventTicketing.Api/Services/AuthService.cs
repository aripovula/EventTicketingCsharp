using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Services;

public class AuthService(AppDbContext db)
{
    public async Task<UserInfo?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user is null)
            return null;

        var result = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return null;

        return new UserInfo(user.Id, user.Name, user.Email, user.Role);
    }

    public async Task<UserInfo?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = new User { Name = request.Name, Email = request.Email, Role = "user" };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return new UserInfo(user.Id, user.Name, user.Email, user.Role);
    }
}
