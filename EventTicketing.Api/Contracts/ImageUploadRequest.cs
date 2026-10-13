using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Contracts;

public record ImageUploadRequest
{
    /// <summary>The image's MIME type: image/jpeg, image/png, image/webp or image/gif.</summary>
    [Required]
    [AllowedValues("image/jpeg", "image/png", "image/webp", "image/gif", ErrorMessage = "Images must be JPEG, PNG, WebP or GIF.")]
    public string ContentType { get; init; } = string.Empty;
}
