using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Api.Services;
using EventTicketing.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Services;

[Trait("Category", "Integration")]
public class AuthServiceTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private AuthService CreateService()
    {
        var db = factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
        return new(db, new AuditLogger(db));
    }

    private async Task<List<AuditLog>> AuditEntriesAsync(string action, string email) =>
        await factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>().AuditLogs
            .Where(a => a.Action == action && a.UserEmail == email)
            .OrderBy(a => a.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

    [Fact]
    public async Task LoginAsync_ReturnsUserInfoForKnownEmail()
    {
        var user = await CreateService().LoginAsync(
            new LoginRequest("john@example.com", UserSeeder.DemoPassword), TestContext.Current.CancellationToken);

        Assert.NotNull(user);
        Assert.Equal(("John Doe", "john@example.com", "user"), (user.Name, user.Email, user.Role));
    }

    [Fact]
    public async Task LoginAsync_RecordsTheSignIn()
    {
        var before = await AuditEntriesAsync(AuditAction.UserLoggedIn, "alex@example.com");

        var user = await CreateService().LoginAsync(
            new LoginRequest("alex@example.com", UserSeeder.DemoPassword), TestContext.Current.CancellationToken);

        var after = await AuditEntriesAsync(AuditAction.UserLoggedIn, "alex@example.com");
        Assert.Equal(before.Count + 1, after.Count);
        Assert.Equal((user!.UserId, user.UserId), (after[^1].EntityId, after[^1].UserId));
    }

    [Fact]
    public async Task LoginAsync_ReturnsNullForUnknownEmail()
    {
        var user = await CreateService().LoginAsync(
            new LoginRequest("nobody@example.com", UserSeeder.DemoPassword), TestContext.Current.CancellationToken);

        Assert.Null(user);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNullForWrongPassword()
    {
        var user = await CreateService().LoginAsync(
            new LoginRequest("john@example.com", "wrong-password"), TestContext.Current.CancellationToken);

        Assert.Null(user);
    }

    [Fact]
    public async Task RegisterAsync_CreatesARegularUserWhoCanThenSignIn()
    {
        var registered = await CreateService().RegisterAsync(
            new RegisterRequest("New User", "registered@example.com", "long-enough-password"), TestContext.Current.CancellationToken);

        var signedIn = await CreateService().LoginAsync(
            new LoginRequest("registered@example.com", "long-enough-password"), TestContext.Current.CancellationToken);

        Assert.NotNull(registered);
        Assert.Equal("user", registered.Role);
        Assert.Equal(registered, signedIn);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsNullForAnAlreadyRegisteredEmail()
    {
        var registered = await CreateService().RegisterAsync(
            new RegisterRequest("Another John", "john@example.com", "long-enough-password"), TestContext.Current.CancellationToken);

        Assert.Null(registered);
    }
}
