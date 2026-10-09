using EventTicketing.Api.Contracts;
using EventTicketing.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Api.Controllers.V1;

[ApiController]
[Route("api/v1/events")]
public class EventsController(EventsService eventsService) : ControllerBase
{
    /// <summary>Lists events with title search, sorting and keyset cursor pagination (meta.nextCursor).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<EventResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List([FromQuery] EventListQuery query, CancellationToken cancellationToken)
    {
        var page = await eventsService.ListAsync(query, cancellationToken);
        if (page is null)
            return BadRequest(new ApiEnvelope(null, [new ApiError("invalid_cursor", "The page cursor is invalid.", "after")]));

        return Ok(new ApiEnvelope(page.Items, [], new { nextCursor = page.NextCursor }));
    }

    /// <summary>Returns a single event by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiEnvelope<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var ev = await eventsService.GetAsync(id, cancellationToken);
        if (ev is null)
            return NotFound(new ApiEnvelope(null, [new ApiError("not_found", "Event not found.")]));

        return Ok(ev);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create(EventRequest request, CancellationToken cancellationToken)
    {
        var ev = await eventsService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = ev.Id }, ev);
    }
}
