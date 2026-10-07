using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class JwtOptionsStartupTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Theory]
    [InlineData("")]
    [InlineData("too-short-key")]
    public void StartupFailsWhenSigningKeyIsMissingOrTooShort(string signingKey)
    {
        var misconfigured = factory.WithWebHostBuilder(builder => builder.UseSetting("Jwt:SigningKey", signingKey));

        var exception = Assert.Throws<OptionsValidationException>(() => misconfigured.CreateClient());
        Assert.Contains("Jwt:SigningKey must be at least 32 bytes", exception.Message);
    }
}
