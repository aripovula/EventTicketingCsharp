using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Api.Contracts;

/// <summary>Cursor paging for lists without search or sort options.</summary>
public record PageQuery
{
    /// <summary>Opaque cursor from the previous page's meta.nextCursor.</summary>
    [FromQuery(Name = "after")]
    public string? After { get; init; }

    /// <summary>Page size, 1–50.</summary>
    [FromQuery(Name = "limit")]
    [Range(1, 50)]
    public int Limit { get; init; } = 20;
}
