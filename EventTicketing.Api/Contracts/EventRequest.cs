using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Contracts;

public record EventRequest : IValidatableObject
{
    [Required, MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; init; } = string.Empty;

    public DateTime StartTime { get; init; }

    public DateTime EndTime { get; init; }

    [Required, MaxLength(200)]
    public string Venue { get; init; } = string.Empty;

    [Required, AllowedValues("Music", "Sports", "Comedy", "Business", "Theater", "Food", "Tech", "Art", "Other")]
    public string EventType { get; init; } = "Other";

    [Range(1, 10_000)]
    public int TotalSeats { get; init; }

    [Range(0, 10_000)]
    public int AvailableSeats { get; init; }

    [Range(0, 10_000_000)]
    public int PriceCents { get; init; }

    [MaxLength(500), Url]
    public string? ImageUrl { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
            yield return new ValidationResult("End time must be after start time.", [nameof(EndTime)]);
        if (AvailableSeats > TotalSeats)
            yield return new ValidationResult("Available seats cannot exceed total seats.", [nameof(AvailableSeats)]);
    }
}
