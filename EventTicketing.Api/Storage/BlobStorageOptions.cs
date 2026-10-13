namespace EventTicketing.Api.Storage;

public class BlobStorageOptions
{
    public const string SectionName = "AzureBlob";

    // A secret in Azure (Key Vault); "UseDevelopmentStorage=true" for Azurite in user-secrets locally.
    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "event-images";

    /// <summary>
    /// Browser origins allowed to PUT images, written to the storage account's CORS rules at startup.
    /// Set for Azurite in development; empty leaves CORS to the infrastructure.
    /// </summary>
    public string[] CorsOrigins { get; set; } = [];
}
