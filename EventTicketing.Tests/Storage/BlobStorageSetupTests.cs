using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EventTicketing.Api.Storage;
using EventTicketing.Tests.Integration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventTicketing.Tests.Storage;

[Trait("Category", "Integration")]
public class BlobStorageSetupTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private BlobServiceClient Blobs => factory.Services.GetRequiredService<BlobServiceClient>();

    [Fact]
    public async Task Startup_CreatesTheContainerWithPublicReadsOfSingleBlobs()
    {
        var container = Blobs.GetBlobContainerClient(factory.Services.GetRequiredService<IOptions<BlobStorageOptions>>().Value.ContainerName);

        var policy = await container.GetAccessPolicyAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(PublicAccessType.Blob, policy.Value.BlobPublicAccess);
    }

    [Fact]
    public async Task Startup_LetsTheConfiguredOriginsPutImages()
    {
        // Both entries are set, so the origins in appsettings.Development.json can't leak in.
        await using var host = factory.WithWebHostBuilder(builder => builder
            .UseSetting("AzureBlob:CorsOrigins:0", "http://localhost:5173")
            .UseSetting("AzureBlob:CorsOrigins:1", "http://127.0.0.1:5173"));
        // Starting the host runs the setup.
        _ = host.Services;

        var properties = await Blobs.GetPropertiesAsync(TestContext.Current.CancellationToken);

        var rule = Assert.Single(properties.Value.Cors);
        Assert.Equal("http://localhost:5173,http://127.0.0.1:5173", rule.AllowedOrigins);
        Assert.Equal("PUT", rule.AllowedMethods);
    }
}
