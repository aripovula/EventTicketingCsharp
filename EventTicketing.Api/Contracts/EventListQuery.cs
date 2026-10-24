using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Api.Contracts;

public record EventListQuery
{
    /// <summary>Case-insensitive search within event titles.</summary>
    [FromQuery(Name = "q")]
    [MaxLength(200)]
    public string? Q { get; init; }

    /// <summary>Sort order: name (default), date or price.</summary>
    [FromQuery(Name = "sort")]
    [RegularExpression("^(name|date|price)$", ErrorMessage = "Sort must be name, date or price.")]
    public string Sort { get; init; } = "name";

    /// <summary>Opaque cursor from the previous page's meta.nextCursor.</summary>
    [FromQuery(Name = "after")]
    public string? After { get; init; }

    /// <summary>Page size, 1–50.</summary>
    [FromQuery(Name = "limit")]
    [Range(1, 50)]
    public int Limit { get; init; } = 20;

    /// <summary>Admins only: also list archived events.</summary>
    [FromQuery(Name = "includeArchived")]
    public bool IncludeArchived { get; init; }
}
