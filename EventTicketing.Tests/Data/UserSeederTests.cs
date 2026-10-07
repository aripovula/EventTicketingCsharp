using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Tests.Integration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Data;

[Trait("Category", "Integration")]
public class UserSeederTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private AppDbContext CreateDb() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task SeedsDemoAccountsOnStartup()
    {
        var roles = await CreateDb().Users
            .OrderBy(u => u.Email)
            .Select(u => new { u.Email, u.Role })
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            [
                new { Email = "admin@example.com", Role = "admin" },
                new { Email = "alex@example.com", Role = "user" },
                new { Email = "jane@example.com", Role = "user" },
                new { Email = "john@example.com", Role = "user" },
            ],
            roles);
    }

    [Fact]
    public async Task SeededAccountsUseDemoPassword()
    {
        var john = await CreateDb().Users.SingleAsync(u => u.Email == "john@example.com", TestContext.Current.CancellationToken);

        var result = new PasswordHasher<User>().VerifyHashedPassword(john, john.PasswordHash, UserSeeder.DemoPassword);

        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public async Task SeedingAgainDoesNotDuplicateUsers()
    {
        var db = CreateDb();
        var countBefore = await db.Users.CountAsync(TestContext.Current.CancellationToken);

        UserSeeder.Seed(db);

        Assert.Equal(countBefore, await db.Users.CountAsync(TestContext.Current.CancellationToken));
    }
}
