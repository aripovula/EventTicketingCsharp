using EventTicketing.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace EventTicketing.Tests.Middleware;

public class EnvelopeExceptionHandlerTests
{
    private static EnvelopeExceptionHandler Build() => new();

    [Fact]
    public async Task LeavesExceptionUnhandledByDefault()
    {
        var context = new DefaultHttpContext();

        var handled = await Build().TryHandleAsync(
            context, new InvalidOperationException("boom"), TestContext.Current.CancellationToken);

        Assert.False(handled);
    }
}
