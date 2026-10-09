using EventTicketing.Api.Contracts;

namespace EventTicketing.Api.Services;

/// <summary>Who is asking for an order: a signed-in user (maybe admin) and/or a guest token holder.</summary>
public record OrderViewer(int? UserId, bool IsAdmin, string? GuestToken);

public class OrdersService
{
    public Task<OrderResponse?> GetAsync(int orderId, OrderViewer viewer, CancellationToken cancellationToken)
    {
        return Task.FromResult<OrderResponse?>(null);
    }
}
