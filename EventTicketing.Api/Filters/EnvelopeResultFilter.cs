using EventTicketing.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EventTicketing.Api.Filters;

public class EnvelopeResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: not ApiEnvelope } result
            && (result.StatusCode ?? StatusCodes.Status200OK) < 400)
        {
            result.Value = new ApiEnvelope(result.Value, []);
        }

        await next();
    }
}
