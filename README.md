# Event Ticketing

A full-stack event ticketing platform: visitors browse events and book tickets, and admins manage the event catalogue. We rebuilt it as a portfolio project to show production-shaped backend patterns behind a real-time API: atomic seat reservation, idempotent booking, Redis caching, rate limiting and SignalR push updates.

The backend is ASP.NET Core 10 (C#) with PostgreSQL through Entity Framework Core; the frontend is React 19 with TypeScript, Vite and Tailwind CSS.

---

## Idempotency

`POST /api/v1/events/{id}/bookings` accepts an optional `Idempotency-Key` header (up to 128 characters). Our frontend sends a fresh UUID each time the booking modal opens and reuses it if the visitor retries from the same modal.

- The first request with a key runs normally, and we remember its response (status code and enveloped JSON body) in the `idempotency_keys` table for **24 hours**.
- A later request with the same key and path gets that response again: no second order, no second seat taken. Because the body is stored as `jsonb`, the replay is the same JSON but may differ in spacing and key order.
- Keys are scoped to `(key, request_path)`, so the same key on another endpoint is a different request.
- Without the header, the request runs normally with no idempotency guarantee.

This protects against double bookings when a network timeout makes the client retry.

For a guest booking, the remembered response includes the guest's order access token, so a retry still gets the link to the order. Orders themselves store only the token's SHA-256 hash; the raw token lives in the remembered response until it expires.

### Known limitation: concurrent in-flight requests

The check is read-then-write: the `[Idempotent]` filter looks the key up, finds nothing, lets the booking run, then stores the response. If two requests with the **same key** arrive at the same moment, both can pass the lookup before either has stored its response, and both book a seat. The second store then fails on the unique index on `(key, request_path)`, and we ignore that failure, so each caller gets its own (different) order.

The window is narrow, but it is a real gap. The fix is to claim the key first: insert a pending row under the unique index *before* running the action, so the second request finds the claim and waits for (or replays) the first response instead of booking again.

## Booking confirmations: transactional outbox + RabbitMQ

When a booking succeeds, we tell the rest of the system with a `BookingConfirmedMessage` on RabbitMQ. Today one consumer logs it; a confirmation email or analytics would be more consumers.

**Why RabbitMQ, not Kafka.** Confirmations are a work queue: each one must be handled once, acknowledged, and redelivered if the worker dies halfway. Per-message acknowledgements, durable queues and dead-letter exchanges are exactly that. We don't need to replay history, which is what Kafka is for. A new consumer binds its own queue to the `ticketing` topic exchange without touching booking code.

**Why an outbox.** Publishing straight from the booking request has a gap: the order commits, then the app crashes (or RabbitMQ is down) before the publish, and the confirmation is lost. Instead, we save the message in an `outbox_messages` row in the **same database transaction** as the order: both commit or neither does. A background `OutboxWorker` then publishes pending rows.

- The worker polls every 5 seconds and marks a row processed only after the broker **confirms** it (publisher confirms, persistent messages, and mandatory routing, so "no queue would receive this" is an error rather than a silent drop).
- A failed publish is retried with exponential backoff (5s, 10s, 20s, 40s). After 5 failed attempts the row is **parked**: left unprocessed, with its last error, for a person to look at.
- Delivery is at-least-once: a crash between the publish and saving the row sends the message again, so consumers must tolerate duplicates (the `OrderId` identifies one).

**Consumer side.** `BookingConfirmationConsumer` acknowledges each message only after handling it. A message it can't read is rejected without requeueing, and the queue's dead-letter exchange (`ticketing.dlx`) moves it to `booking-confirmed.dead` for inspection instead of redelivering it forever.

`GET /health/ready` checks RabbitMQ alongside PostgreSQL and Redis.

## Audit logging

We keep an audit trail in the `audit_logs` table:

| Action | Recorded when |
|---|---|
| `EventCreated`, `EventUpdated`, `EventDeleted` | an admin changes the catalogue |
| `TicketBooked` | anyone books a ticket, guests included |
| `UserLoggedIn` | a sign-in succeeds |
| `UserLoginFailed` | a sign-in fails, for an unknown email or a wrong password |

Each row records the action, the entity it touched (`entity_type`, `entity_id`), the acting user's id and email when we know them, a little context as `jsonb` (such as the event title), and `created_at`.

- The audit row is saved in the **same transaction** as the change it records, so there is never a change without its entry, or an entry for a change that rolled back.
- The table has no foreign keys, so the trail outlives the events and users it mentions.
- We don't have an endpoint for reading the trail yet; we query it in PostgreSQL:

```sql
SELECT created_at, action, entity_type, entity_id, user_email, details
FROM audit_logs ORDER BY id DESC LIMIT 20;
```

## Payments: Stripe Elements + PaymentIntents

Booking a paid ticket charges a card through Stripe (test mode). Free events ($0) skip payment altogether, and a paid price must be at least $0.50, the least Stripe can charge. The card number never touches our servers: Stripe Elements renders the card field in Stripe's own cross-origin iframe, and the browser sends us only a PaymentMethod id (`pm_…`). That keeps our API out of PCI DSS scope.

The order of operations is what makes it safe:

1. **Reserve the seat first**, with the atomic `UPDATE … WHERE available_seats > 0`. A buyer who loses the race for the last seat gets `409 sold_out` *before Stripe is called*, so there is no charge to undo.
2. **Charge the card**: one PaymentIntent, created and confirmed in a single call. The booking's `Idempotency-Key` (or a fresh key when the client sends none) goes to Stripe as *its* idempotency key, so a retried request can't charge twice.
3. **Save the order**, its outbox message and its audit entry in one transaction, with the PaymentIntent id on the order.

When something fails, we compensate:

- **Card declined** → we put the seat back and answer `402 payment_declined` with Stripe's message. The frontend then uses a new idempotency key, so trying another card isn't answered with the stored decline.
- **Stripe unreachable** → we put the seat back and the request fails.
- **Saving the order fails after the charge** → we refund the PaymentIntent and put the seat back. If the refund itself fails, we log an error for a person to refund by hand, and still report the original failure.

Cards that need 3-D Secure are declined for now: we don't run Stripe's browser verification step.

### Running without Stripe keys

`Payments:Provider=Fake` swaps Stripe for a fake gateway that uses Stripe's own test ids: `pm_card_visa` is approved and `pm_card_chargeDeclined` is declined. The API refuses to start with the fake in Production. Without `VITE_STRIPE_PUBLISHABLE_KEY`, the booking form offers those two test cards instead of Stripe's card field. Integration tests and the CI E2E jobs run this way, so they need no Stripe secrets.
