using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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

    [Fact]
    public async Task Register_ReturnsTheNewUserWithoutAnyPasswordData()
    {
        var response = await RegisterAsync("Body Check", "body.check@example.com", "long-enough");

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var data = JsonDocument.Parse(body).RootElement.GetProperty("data");
        Assert.Equal("Body Check", data.GetProperty("name").GetString());
        Assert.Equal("user", data.GetProperty("role").GetString());
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithTakenEmail_Returns409EmailTaken()
    {
        var response = await RegisterAsync("Another Jane", "jane@example.com", "long-enough");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.GetProperty("errors")[0];
        Assert.Equal("email_taken", error.GetProperty("code").GetString());
        Assert.Equal("email", error.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Register_ConcurrentlyWithTheSameEmail_CreatesExactlyOneAccount()
    {
        var attempts = Enumerable.Range(0, 10).Select(i =>
            factory.CreateClient().PostAsJsonAsync(
                "/api/v1/auth/register",
                new { name = $"Racer {i}", email = "race@example.com", password = "long-enough" },
                TestContext.Current.CancellationToken));

        var statuses = (await Task.WhenAll(attempts)).Select(r => r.StatusCode).ToList();

        Assert.Equal(1, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(9, statuses.Count(s => s == HttpStatusCode.Conflict));
    }
}
