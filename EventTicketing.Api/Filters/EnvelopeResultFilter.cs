using Microsoft.AspNetCore.Mvc.Filters;

namespace EventTicketing.Api.Filters;

public class EnvelopeResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        await next();
    }
}
