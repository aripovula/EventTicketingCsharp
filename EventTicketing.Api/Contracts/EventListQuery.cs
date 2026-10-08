using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Contracts;

public record EventListQuery
{
    /// <summary>Case-insensitive search within event titles.</summary>
    [MaxLength(200)]
    public string? Q { get; init; }

    /// <summary>Sort order: name (default), date or price.</summary>
    [RegularExpression("^(name|date|price)$", ErrorMessage = "Sort must be name, date or price.")]
    public string Sort { get; init; } = "name";

    /// <summary>Opaque cursor from the previous page's meta.nextCursor.</summary>
    public string? After { get; init; }

    /// <summary>Page size, 1–50.</summary>
    [Range(1, 50)]
    public int Limit { get; init; } = 20;
}
