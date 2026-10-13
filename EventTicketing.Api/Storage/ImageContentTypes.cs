namespace EventTicketing.Api.Storage;

/// <summary>The image types admins may upload, with the file extension each blob gets.</summary>
public static class ImageContentTypes
{
    public static readonly IReadOnlyDictionary<string, string> Extensions = new Dictionary<string, string>
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };
}
