using EventTicketing.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace EventTicketing.Api.Data;

public static class UserSeeder
{
    public const string DemoPassword = "Password";

    public static void Seed(AppDbContext db)
    {
        if (db.Users.Any()) return;

        var hasher = new PasswordHasher<User>();

        var users = new[]
        {
            new User { Name = "John Doe",     Email = "john@example.com",  Role = "user"  },
            new User { Name = "Jane Doer",    Email = "jane@example.com",  Role = "user"  },
            new User { Name = "Alex Johnson", Email = "alex@example.com",  Role = "user"  },
            new User { Name = "Admin",        Email = "admin@example.com", Role = "admin" },
        };

        foreach (var user in users)
            user.PasswordHash = hasher.HashPassword(user, DemoPassword);

        db.Users.AddRange(users);
        db.SaveChanges();
    }
}
