namespace EventTicketing.Api.Contracts;

/// <param name="UploadUrl">Write-only SAS URL: PUT the image here (with <c>x-ms-blob-type: BlockBlob</c>) before <paramref name="ExpiresAt"/>.</param>
/// <param name="ImageUrl">Where the image can be read once uploaded; store it as the event's imageUrl.</param>
public record ImageUploadResponse(string UploadUrl, string ImageUrl, DateTimeOffset ExpiresAt);
