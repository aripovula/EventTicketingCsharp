namespace EventTicketing.Api.Contracts;

public record ApiError(string Code, string Detail, string? Field = null);
