using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EventTicketing.Api.Storage;
using EventTicketing.Tests.Integration;
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

}
