using EventTicketing.Api.Contracts;
using System.Text.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Messaging;
using EventTicketing.Api.Models;
using EventTicketing.Api.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EventTicketing.Api.Services;

public class BookingService(AppDbContext db, IOptions<RabbitMqOptions> rabbitMq, AuditLogger audit, IPaymentGateway payments)
{
    /// <param name="idempotencyKey">Passed on to Stripe, so a retried request can't charge the card twice.</param>
    public async Task<BookingResult> BookAsync(int eventId, BookingRequest request, int? userId, string idempotencyKey, CancellationToken cancellationToken)
    {
        // Check and decrement in ONE statement: the row lock queues concurrent buyers,
        // so the second one for the last seat matches zero rows instead of overselling.
        var reserved = await db.Events
            .Where(e => e.Id == eventId && e.AvailableSeats > 0)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.AvailableSeats, e => e.AvailableSeats - 1), cancellationToken);

        if (reserved == 0)
        {
            var exists = await db.Events.AnyAsync(e => e.Id == eventId, cancellationToken);
            return new BookingResult(exists ? BookingStatus.SoldOut : BookingStatus.EventNotFound);
        }

        var ev = await db.Events.SingleAsync(e => e.Id == eventId, cancellationToken);
        // Charge only after the seat is ours: a buyer who loses the race for the last seat gets
        // "sold out" before Stripe is called, so there's nothing to refund.
        string paymentId;
        try
        {
            paymentId = await payments.ChargeAsync(ev.PriceCents, request.PaymentMethodId, idempotencyKey, cancellationToken);
        }
        catch (PaymentDeclinedException e)
        {
            await ReleaseSeatAsync(eventId);
            return new BookingResult(BookingStatus.PaymentDeclined, Error: e.Message);
        }
        catch
        {
            await ReleaseSeatAsync(eventId);
            throw;
        }

        // Guests have no account to find the order by, so they get a one-time lookup secret.
        var guestToken = userId is null ? SecureToken.Generate() : null;
        var order = new Order
        {
            EventId = eventId,
            UserId = userId,
            Email = request.Email,
            PriceCents = ev.PriceCents,
            BookedAt = DateTime.UtcNow,
            GuestAccessTokenHash = guestToken is null ? null : SecureToken.Hash(guestToken),
            StripePaymentIntentId = paymentId,
        };
        // The confirmation message commits or rolls back with the order, so a crash or a
        // RabbitMQ outage can't leave a booking whose confirmation is never sent.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = nameof(BookingConfirmedMessage),
            Payload = JsonSerializer.Serialize(new BookingConfirmedMessage(
                order.Id, eventId, ev.Title, order.Email, order.PriceCents, order.BookedAt)),
            Exchange = rabbitMq.Value.Exchange,
            RoutingKey = BookingConfirmedMessage.RoutingKey,
            CreatedAt = DateTime.UtcNow,
        });
        audit.Record(AuditAction.TicketBooked, "Order", order.Id, new AuditActor(userId, request.Email),
            new { EventId = eventId, EventTitle = ev.Title, order.PriceCents });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new BookingResult(BookingStatus.Booked,
            new OrderResponse(order.Id, eventId, ev.Title, ev.Venue, ev.EventType, ev.StartTime, ev.EndTime, order.Email, order.PriceCents, order.BookedAt, guestToken));
    }

    // Puts a reserved seat back on sale. It must happen even if the request was cancelled meanwhile.
    private Task ReleaseSeatAsync(int eventId) =>
        db.Events
            .Where(e => e.Id == eventId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.AvailableSeats, e => e.AvailableSeats + 1), CancellationToken.None);
}
