using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Contracts;

public record BookingRequest
{
    /// <summary>Where the ticket goes; required for guests and signed-in users alike.</summary>
    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; init; } = string.Empty;
}
