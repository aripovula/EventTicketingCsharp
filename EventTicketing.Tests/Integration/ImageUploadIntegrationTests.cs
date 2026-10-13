using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class ImageUploadIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private const string Url = "/api/v1/admin/uploads/sas-url";

    private async Task<HttpClient> SignedInAsync(string email)
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Password" }, TestContext.Current.CancellationToken);
        return client;
    }

    [Fact]
    public async Task SasUrl_WithoutSigningIn_Returns401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(Url, new { contentType = "image/png" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SasUrl_ForARegularUser_Returns403()
    {
        var client = await SignedInAsync("jane@example.com");

        var response = await client.PostAsJsonAsync(Url, new { contentType = "image/png" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SasUrl_ForAnAdmin_ReturnsAnUploadUrlAndTheImageUrl()
    {
        var client = await SignedInAsync("admin@example.com");

        var response = await client.PostAsJsonAsync(Url, new { contentType = "image/webp" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.GetProperty("data");
        var imageUrl = data.GetProperty("imageUrl").GetString()!;
        Assert.EndsWith(".webp", imageUrl);
        Assert.StartsWith(imageUrl + "?", data.GetProperty("uploadUrl").GetString());
        Assert.True(data.GetProperty("expiresAt").GetDateTimeOffset() > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task SasUrl_ForSomethingOtherThanAnImage_Returns400()
    {
        var client = await SignedInAsync("admin@example.com");

        var response = await client.PostAsJsonAsync(Url, new { contentType = "text/html" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"field\":\"contentType\"", body);
        Assert.Contains("Images must be JPEG, PNG, WebP or GIF.", body);
    }
}
