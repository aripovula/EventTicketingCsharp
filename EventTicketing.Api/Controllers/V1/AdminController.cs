using EventTicketing.Api.Contracts;
using EventTicketing.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Api.Controllers.V1;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/v1/admin")]
public class AdminController(OrdersService ordersService, EventsService eventsService) : ControllerBase
{
    /// <summary>Lists every order, newest first, with cursor pagination (meta.nextCursor). Admin only.</summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<OrderResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Orders([FromQuery] PageQuery query, CancellationToken cancellationToken)
    {
        var page = await ordersService.ListAsync(null, query, cancellationToken);
        if (page is null)
            return BadRequest(new ApiEnvelope(null, [new ApiError("invalid_cursor", "The page cursor is invalid.", "after")]));

        return Ok(new ApiEnvelope(page.Items, [], new { nextCursor = page.NextCursor }));
    }

    /// <summary>Seats sold, seats remaining and revenue for every event, in date order. Admin only.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<EventSummary>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        return Ok(await eventsService.SummaryAsync(cancellationToken));
    }
}
