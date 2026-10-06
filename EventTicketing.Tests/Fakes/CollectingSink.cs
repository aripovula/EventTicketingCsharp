using Serilog.Core;
using Serilog.Events;

namespace EventTicketing.Tests.Fakes;

public class CollectingSink : ILogEventSink
{
    public List<LogEvent> Events { get; } = [];

    public void Emit(LogEvent logEvent) => Events.Add(logEvent);
}
