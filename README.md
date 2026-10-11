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
