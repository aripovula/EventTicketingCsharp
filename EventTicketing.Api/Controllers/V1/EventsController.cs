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
        return Ok(new ApiEnvelope(page.Items, [], new { nextCursor = page.NextCursor }));
    }
}
