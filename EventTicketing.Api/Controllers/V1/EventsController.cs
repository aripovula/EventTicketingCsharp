using EventTicketing.Api.Contracts;
using EventTicketing.Api.Filters;
using EventTicketing.Api.Hubs;
using EventTicketing.Api.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;

namespace EventTicketing.Api.Controllers.V1;

[ApiController]
[Route("api/v1/events")]
public class EventsController(EventsService eventsService, BookingService bookingService, IHubContext<TicketingHub> hub) : ControllerBase
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

    /// <summary>Creates an event. Admin only.</summary>
    [Authorize(Roles = "admin")]
    [HttpPost]
    [ProducesResponseType(typeof(ApiEnvelope<EventResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventRequest request, CancellationToken cancellationToken)
    {
        var ev = await eventsService.CreateAsync(request, AuditActor.FromClaims(User), cancellationToken);
        await hub.Clients.All.SendAsync(TicketingEvents.EventCreated, ev.Id, CancellationToken.None);
        return CreatedAtAction(nameof(Get), new { id = ev.Id }, ev);
    }

    /// <summary>Updates an event. Admin only.</summary>
    [Authorize(Roles = "admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventRequest request, CancellationToken cancellationToken)
    {
        if (!await eventsService.UpdateAsync(id, request, AuditActor.FromClaims(User), cancellationToken))
            return NotFound(new ApiEnvelope(null, [new ApiError("not_found", "Event not found.")]));

        await hub.Clients.All.SendAsync(TicketingEvents.EventUpdated, id, CancellationToken.None);
        return NoContent();
    }

    /// <summary>Deletes an event. Admin only.</summary>
    [Authorize(Roles = "admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!await eventsService.DeleteAsync(id, AuditActor.FromClaims(User), cancellationToken))
            return NotFound(new ApiEnvelope(null, [new ApiError("not_found", "Event not found.")]));

        await hub.Clients.All.SendAsync(TicketingEvents.EventDeleted, id, CancellationToken.None);
        return NoContent();
    }

    /// <summary>Books and pays for one seat, as a guest or signed-in user. The seat is reserved atomically before the card is charged, so the last seat can never be sold twice; a declined card returns 402 and puts the seat back.</summary>
    [HttpPost("{id:int}/bookings")]
    [EnableRateLimiting(RateLimitingOptions.BookingPolicy)]
    [Idempotent]
    [ProducesResponseType(typeof(ApiEnvelope<OrderResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Book(
        int id,
        BookingRequest request,
        // Read by the [Idempotent] filter; declared here so Swagger documents the header.
        [FromHeader(Name = IdempotentAttribute.HeaderName)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // Without a key from the client, this request still gets one, so Stripe can't charge it twice.
        var result = await bookingService.BookAsync(
            id, request, userId is null ? null : int.Parse(userId), idempotencyKey ?? Guid.NewGuid().ToString(), cancellationToken);
        if (result.Status == BookingStatus.SoldOut)
            return Conflict(new ApiEnvelope(null, [new ApiError("sold_out", "Sorry, this event just sold out.")]));
        if (result.Status == BookingStatus.EventNotFound)
            return NotFound(new ApiEnvelope(null, [new ApiError("not_found", "Event not found.")]));
        if (result.Status == BookingStatus.PaymentMethodRequired)
            return BadRequest(new ApiEnvelope(null,
                [new ApiError("validation_failed", "A card is required for this event.", "paymentMethodId")]));
        if (result.Status == BookingStatus.PaymentDeclined)
            return StatusCode(StatusCodes.Status402PaymentRequired,
                new ApiEnvelope(null, [new ApiError("payment_declined", result.Error!)]));

        // The seat is already taken, so tell open pages even if this request was cancelled.
        await hub.Clients.All.SendAsync(TicketingEvents.BookingMade, id, CancellationToken.None);
        return CreatedAtAction(nameof(OrdersController.Get), "Orders", new { id = result.Order!.Id }, result.Order);
    }
}
