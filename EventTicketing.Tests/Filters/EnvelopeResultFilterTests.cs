using EventTicketing.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace EventTicketing.Tests.Filters;

public class EnvelopeResultFilterTests
{
    private static ResultExecutingContext BuildContext(IActionResult result)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        return new ResultExecutingContext(actionContext, [], result, controller: new object());
    }

    private static Task<ResultExecutedContext> Next(ResultExecutingContext context) =>
        Task.FromResult(new ResultExecutedContext(context, [], context.Result, context.Controller));

    [Fact]
    public async Task CallsNextResultFilter()
    {
        var nextCalled = false;
        var context = BuildContext(new OkObjectResult("payload"));

        await new EnvelopeResultFilter().OnResultExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Next(context);
        });

        Assert.True(nextCalled);
    }
}
