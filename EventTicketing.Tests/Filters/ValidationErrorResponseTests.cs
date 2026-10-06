using EventTicketing.Api.Contracts;
using EventTicketing.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

namespace EventTicketing.Tests.Filters;

public class ValidationErrorResponseTests
{
    private static ActionContext BuildContext() =>
        new(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

    [Fact]
    public void ReturnsBadRequestEnvelopeWithoutData()
    {
        var context = BuildContext();
        context.ModelState.AddModelError("Email", "The Email field is required.");

        var result = Assert.IsType<BadRequestObjectResult>(ValidationErrorResponse.Create(context));

        var envelope = Assert.IsType<ApiEnvelope>(result.Value);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public void MapsEachModelErrorToCamelCasedFieldError()
    {
        var context = BuildContext();
        context.ModelState.AddModelError("Email", "The Email field is required.");
        context.ModelState.AddModelError("TotalSeats", "Must be positive.");

        var result = (BadRequestObjectResult)ValidationErrorResponse.Create(context);

        var envelope = (ApiEnvelope)result.Value!;
        Assert.Equal(
            [
                new ApiError("validation_failed", "The Email field is required.", "email"),
                new ApiError("validation_failed", "Must be positive.", "totalSeats"),
            ],
            envelope.Errors);
    }
}
