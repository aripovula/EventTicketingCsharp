using EventTicketing.Api.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventTicketing.Tests.Integration;

[Trait("Category", "Integration")]
public class BlobStorageOptionsStartupTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public void StartupFailsWhenTheConnectionStringIsMissing()
    {
        var host = factory.WithWebHostBuilder(builder => builder.UseSetting("AzureBlob:ConnectionString", ""));

        var exception = Assert.Throws<OptionsValidationException>(() => host.Services.GetRequiredService<IOptions<BlobStorageOptions>>().Value);

        Assert.Contains("AzureBlob:ConnectionString is required", exception.Message);
    }
}
