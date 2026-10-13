using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace EventTicketing.Api.Storage;

/// <summary>
/// Creates the images container (anyone may read a blob, nobody may list them) and, when origins are
/// configured, lets those browsers PUT to the storage account.
/// </summary>
public class BlobStorageSetup(BlobServiceClient blobs, IOptions<BlobStorageOptions> options, ILogger<BlobStorageSetup> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await blobs.GetBlobContainerClient(options.Value.ContainerName)
                .CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

            if (options.Value.CorsOrigins.Length > 0)
            {
                var properties = (await blobs.GetPropertiesAsync(cancellationToken)).Value;
                properties.Cors =
                [
                    new BlobCorsRule
                    {
                        AllowedOrigins = string.Join(',', options.Value.CorsOrigins),
                        AllowedMethods = "PUT",
                        AllowedHeaders = "content-type,x-ms-blob-type,x-ms-blob-content-type,x-ms-version,x-ms-date",
                        ExposedHeaders = "",
                        MaxAgeInSeconds = 3600,
                    },
                ];
                await blobs.SetPropertiesAsync(properties, cancellationToken);
            }
        }
        catch (Exception e)
        {
            // Image uploads fail until storage is reachable; the rest of the API works without it.
            logger.LogError(e, "Blob storage setup failed; image uploads won't work");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
