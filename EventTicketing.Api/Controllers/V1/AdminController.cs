using EventTicketing.Api.Contracts;
using EventTicketing.Api.Services;
using EventTicketing.Api.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Api.Controllers.V1;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/v1/admin")]
public class AdminController(OrdersService ordersService, EventsService eventsService, BlobImageStorage images) : ControllerBase
{
    /// <summary>Lists every order, newest first, with cursor pagination (meta.nextCursor). Admin only.</summary>
    /// <param name="query">Cursor paging.</param>
    /// <param name="eventId">Only orders for this event.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<OrderResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Orders([FromQuery] PageQuery query, [FromQuery] int? eventId, CancellationToken cancellationToken)
    {
        var page = await ordersService.ListAsync(null, query, cancellationToken, eventId);
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

    /// <summary>
    /// Issues a write-only upload URL for one event image, valid for 5 minutes. The browser PUTs the
    /// image straight to Blob Storage, then saves the returned imageUrl on the event. Admin only.
    /// </summary>
    [HttpPost("uploads/sas-url")]
    [ProducesResponseType(typeof(ApiEnvelope<ImageUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult ImageUploadUrl(ImageUploadRequest request) =>
        Ok(images.CreateUploadUrl(request.ContentType));
}
