namespace EventTicketing.Api.Contracts;

public record ApiEnvelope(object? Data, IReadOnlyList<ApiError> Errors, object? Meta = null);
