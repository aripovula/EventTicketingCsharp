namespace EventTicketing.Api.Contracts;

/// <summary>
/// Typed twin of <see cref="ApiEnvelope"/> used only in [ProducesResponseType], so the
/// OpenAPI spec shows the real shape of <c>data</c> for each endpoint.
/// </summary>
public record ApiEnvelope<T>(T? Data, IReadOnlyList<ApiError> Errors, object? Meta = null);
