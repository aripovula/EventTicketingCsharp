using System.Net;
using System.Net.Http.Json;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class AuthRegisterIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private Task<HttpResponseMessage> RegisterAsync(string name, string email, string password) =>
        _client.PostAsJsonAsync("/api/v1/auth/register", new { name, email, password }, TestContext.Current.CancellationToken);

    [Theory]
    [InlineData("A", "valid@example.com", "long-enough", "name")]
    [InlineData("Valid Name", "not-an-email", "long-enough", "email")]
    [InlineData("Valid Name", "valid@example.com", "short12", "password")]
    public async Task Register_WithInvalidField_Returns400ForThatField(string name, string email, string password, string field)
    {
        var response = await RegisterAsync(name, email, password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains($"\"field\":\"{field}\"", body);
    }

    [Fact]
    public async Task Register_WithValidRequest_Returns201()
    {
        var response = await RegisterAsync("New User", "new.user@example.com", "long-enough");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
