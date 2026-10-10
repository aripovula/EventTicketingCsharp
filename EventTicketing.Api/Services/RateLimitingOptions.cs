namespace EventTicketing.Api.Services;

/// <summary>Per-IP request limits; each limit counts requests per minute.</summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int GlobalPermitLimit { get; set; } = 100;
}
