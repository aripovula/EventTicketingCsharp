using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Services;

public class AuthService(AppDbContext db)
{
    public async Task<UserInfo?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user is null)
            return null;

        return new UserInfo(user.Id, user.Name, user.Email, user.Role);
    }
}
