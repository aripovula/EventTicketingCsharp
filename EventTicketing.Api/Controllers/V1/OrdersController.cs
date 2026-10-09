using System.Security.Claims;
using EventTicketing.Api.Contracts;
using EventTicketing.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Api.Controllers.V1;

[ApiController]
[Route("api/v1/orders")]
public class OrdersController(OrdersService ordersService) : ControllerBase
{
    public const string GuestTokenHeader = "X-Order-Token";

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, [FromHeader(Name = GuestTokenHeader)] string? guestToken, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var viewer = new OrderViewer(userId is null ? null : int.Parse(userId), User.IsInRole("admin"), guestToken);
        var order = await ordersService.GetAsync(id, viewer, cancellationToken);
        // Same answer for "missing" and "not yours", so order ids can't be probed.
        if (order is null)
            return NotFound(new ApiEnvelope(null, [new ApiError("not_found", "Order not found.")]));

        return Ok(order);
    }
}
