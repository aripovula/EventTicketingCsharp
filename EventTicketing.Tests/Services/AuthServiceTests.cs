using EventTicketing.Api.Contracts;
using EventTicketing.Api.Data;
using EventTicketing.Api.Services;
using EventTicketing.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Services;

[Trait("Category", "Integration")]
public class AuthServiceTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private AuthService CreateService() =>
        new(factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>());

    [Fact]
    public async Task LoginAsync_ReturnsUserInfoForKnownEmail()
    {
        var user = await CreateService().LoginAsync(
            new LoginRequest("john@example.com", UserSeeder.DemoPassword), TestContext.Current.CancellationToken);

        Assert.NotNull(user);
        Assert.Equal(("John Doe", "john@example.com", "user"), (user.Name, user.Email, user.Role));
    }

    [Fact]
    public async Task LoginAsync_ReturnsNullForUnknownEmail()
    {
        var user = await CreateService().LoginAsync(
            new LoginRequest("nobody@example.com", UserSeeder.DemoPassword), TestContext.Current.CancellationToken);

        Assert.Null(user);
    }
}
