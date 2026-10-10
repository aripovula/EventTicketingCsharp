namespace EventTicketing.Api.Services;

/// <summary>Per-IP request limits; each limit counts requests per minute.</summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Name of the stricter limiter policy on booking.</summary>
    public const string BookingPolicy = "booking";

    public int GlobalPermitLimit { get; set; } = 100;

    public int BookingPermitLimit { get; set; } = 5;
}
