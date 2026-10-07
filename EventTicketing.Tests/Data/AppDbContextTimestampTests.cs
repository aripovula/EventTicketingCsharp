using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Data;

[Trait("Category", "Integration")]
public class AppDbContextTimestampTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private static User NewUser(string email) =>
        new() { Name = "Test User", Email = email, PasswordHash = "hash" };

    [Fact]
    public async Task StampsCreatedAtAndUpdatedAtOnInsert()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var before = DateTime.UtcNow;

        var user = NewUser("insert@example.com");
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.True(user.CreatedAt >= before);
        Assert.Equal(user.CreatedAt, user.UpdatedAt);
    }

    [Fact]
    public async Task UpdatesOnlyUpdatedAtOnModify()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = NewUser("modify@example.com");
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var createdAt = user.CreatedAt;

        user.Name = "Renamed User";
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(createdAt, user.CreatedAt);
        Assert.True(user.UpdatedAt > createdAt);
    }
}
