using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using EventTicketing.Api.Contracts;
using Microsoft.Extensions.Options;

namespace EventTicketing.Api.Storage;

/// <summary>
/// Hands out short-lived, write-only upload URLs, so images go from the browser straight to Blob
/// Storage and never pass through the API.
/// </summary>
public class BlobImageStorage(BlobServiceClient blobs, IOptions<BlobStorageOptions> options)
{
    public static readonly TimeSpan UploadUrlLifetime = TimeSpan.FromMinutes(5);

    public ImageUploadResponse CreateUploadUrl(string contentType)
    {
        // We name the blob, so a client's file name can't clash with or overwrite another image.
        var blob = blobs.GetBlobContainerClient(options.Value.ContainerName)
            .GetBlobClient($"{Guid.NewGuid():N}{ImageContentTypes.Extensions[contentType]}");
        var expiresAt = DateTimeOffset.UtcNow.Add(UploadUrlLifetime);

        // Create + write only: a leaked URL can't read, list or delete anything, and stops working in minutes.
        var uploadUrl = blob.GenerateSasUri(BlobSasPermissions.Create | BlobSasPermissions.Write, expiresAt);

        return new ImageUploadResponse(uploadUrl.ToString(), blob.Uri.ToString(), expiresAt);
    }
}
