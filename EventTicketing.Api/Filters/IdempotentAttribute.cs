using System.Text.Json;
using EventTicketing.Api.Contracts;
using EventTicketing.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EventTicketing.Api.Filters;

/// <summary>
/// Opt-in idempotency for POST actions: a request carrying an <c>Idempotency-Key</c> header that was
/// already answered gets the remembered response again instead of running the action twice.
/// Without the header the request runs normally.
/// </summary>
/// <remarks>
/// Read-then-write: two requests with the same key arriving at the same moment can both run the action.
/// The second store then fails on the unique index and is ignored.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    public const string HeaderName = "Idempotency-Key";
    private const int MaxKeyLength = 128;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var key = http.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(key))
        {
            await next();
            return;
        }

        if (key.Length > MaxKeyLength)
        {
            context.Result = new BadRequestObjectResult(new ApiEnvelope(null,
                [new ApiError("invalid_idempotency_key", $"Idempotency-Key must be at most {MaxKeyLength} characters.", HeaderName)]));
            return;
        }

        var path = http.Request.Path.Value ?? string.Empty;
        var service = http.RequestServices.GetRequiredService<IdempotencyService>();
        var remembered = await service.GetAsync(key, path, http.RequestAborted);
        if (remembered is not null)
        {
            context.Result = new ContentResult
            {
                StatusCode = remembered.StatusCode,
                Content = remembered.ResponseBody,
                ContentType = "application/json",
            };
            return;
        }

        var executed = await next();
        if (executed.Exception is not null || executed.Result is not ObjectResult result)
            return;

        // Remember the body in the shape the envelope filter is about to send.
        var body = result.Value as ApiEnvelope ?? new ApiEnvelope(result.Value, []);
        var json = JsonSerializer.Serialize(body, http.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions);
        try
        {
            // The action already ran, so remember its answer even if the client has gone.
            await service.StoreAsync(key, path, result.StatusCode ?? StatusCodes.Status200OK, json, CancellationToken.None);
        }
        catch (DbUpdateException)
        {
            // A concurrent request with the same key stored its answer first (see remarks).
        }
    }
}
