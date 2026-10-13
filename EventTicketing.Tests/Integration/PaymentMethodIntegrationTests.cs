using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class PaymentMethodIntegrationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private const string Url = "/api/v1/auth/me/payment-method";

    // A fresh account per test, so no test sees another's saved card.
    private async Task<HttpClient> SignedInNewUserAsync()
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/v1/auth/register", new { name = "Card Holder", email, password = "Password1" }, TestContext.Current.CancellationToken);
        await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Password1" }, TestContext.Current.CancellationToken);
        return client;
    }

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;

    [Fact]
    public async Task Put_WithoutSigningIn_Returns401()
    {
        var response = await factory.CreateClient().PutAsJsonAsync(Url, new { paymentMethodId = "pm_card_visa" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Put_SavesTheCardAndReturnsWhatWeShow()
    {
        var client = await SignedInNewUserAsync();

        var response = await client.PutAsJsonAsync(Url, new { paymentMethodId = "pm_card_visa" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var card = (await BodyAsync(response)).GetProperty("data");
        Assert.Equal("visa", card.GetProperty("brand").GetString());
        Assert.Equal("4242", card.GetProperty("last4").GetString());
        // Only display fields leave the API; the Stripe ids stay on the server.
        Assert.False(card.TryGetProperty("stripePaymentMethodId", out _));
    }

    [Fact]
    public async Task Put_WithoutAPaymentMethod_Returns400()
    {
        var client = await SignedInNewUserAsync();

        var response = await client.PutAsJsonAsync(Url, new { }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
