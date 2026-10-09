using System.Net;
using System.Net.Http.Json;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class BookingIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Book_WithMalformedEmail_Returns400ForEmail()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/events/1/bookings", new { email = "not-an-email" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"field\":\"email\"", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
