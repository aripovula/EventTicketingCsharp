using EventTicketing.Api.Contracts;
using EventTicketing.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Api.Controllers.V1;

[ApiController]
[Route("api/v1/events")]
public class EventsController(EventsService eventsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] EventListQuery query, CancellationToken cancellationToken)
    {
        var page = await eventsService.ListAsync(query, cancellationToken);
        if (page is null)
            return BadRequest(new ApiEnvelope(null, [new ApiError("invalid_cursor", "The page cursor is invalid.", "after")]));

        return Ok(new ApiEnvelope(page.Items, [], new { nextCursor = page.NextCursor }));
    }
}
