using System.Diagnostics;

namespace EventTicketing.Api.Telemetry;

/// <summary>The source of the spans our own code starts (the libraries' spans come from their instrumentation).</summary>
public static class Tracing
{
    public const string SourceName = "EventTicketing.Api";

    public static readonly ActivitySource Source = new(SourceName);
}
