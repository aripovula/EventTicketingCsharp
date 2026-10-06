using EventTicketing.Api.Contracts;
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

    [Fact]
    public async Task WrapsSuccessfulObjectResultInEnvelope()
    {
        var context = BuildContext(new OkObjectResult("payload"));

        await new EnvelopeResultFilter().OnResultExecutionAsync(context, () => Next(context));

        var envelope = Assert.IsType<ApiEnvelope>(((ObjectResult)context.Result).Value);
        Assert.Equal("payload", envelope.Data);
        Assert.Empty(envelope.Errors);
    }

    [Fact]
    public async Task DoesNotWrapExistingEnvelope()
    {
        var existing = new ApiEnvelope("payload", []);
        var context = BuildContext(new OkObjectResult(existing));

        await new EnvelopeResultFilter().OnResultExecutionAsync(context, () => Next(context));

        Assert.Same(existing, ((ObjectResult)context.Result).Value);
    }

    [Fact]
    public async Task DoesNotWrapErrorResult()
    {
        var context = BuildContext(new BadRequestObjectResult("bad"));

        await new EnvelopeResultFilter().OnResultExecutionAsync(context, () => Next(context));

        Assert.Equal("bad", ((ObjectResult)context.Result).Value);
    }
}
