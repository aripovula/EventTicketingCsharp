using System.Text.Json;
using EventTicketing.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Api.Filters;

public static class ValidationErrorResponse
{
    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .SelectMany(entry => entry.Value!.Errors.Select(error => new ApiError(
                "validation_failed",
                error.ErrorMessage,
                JsonNamingPolicy.CamelCase.ConvertName(entry.Key))))
            .ToList();

        return new BadRequestObjectResult(new ApiEnvelope(null, errors));
    }
}
