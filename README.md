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

### Saved cards

Signed-in users can save a card on their profile page (`PUT /api/v1/auth/me/payment-method`). We attach it to a Stripe Customer, which we create the first time a user saves a card. Stripe keeps the card itself. Our `payment_methods` table holds only the Stripe customer and PaymentMethod ids, plus the brand, last four digits and expiry we show. The API returns only those display fields, never the Stripe ids.

The booking modal then offers "Visa ending in 4242" first. Choosing it sends `useSavedPaymentMethod: true` instead of a PaymentMethod id, and the server looks the card up by the signed-in user, so nobody can pay with someone else's card. Saving a new card keeps the old row but stops it being the default.

### Running without Stripe keys

`Payments:Provider=Fake` swaps Stripe for a fake gateway that uses Stripe's own test ids: `pm_card_visa` is approved and `pm_card_chargeDeclined` is declined. The API refuses to start with the fake in Production. Without `VITE_STRIPE_PUBLISHABLE_KEY`, the booking form offers those two test cards instead of Stripe's card field. Integration tests and the CI E2E jobs run this way, so they need no Stripe secrets.

## Image uploads: write-only SAS URLs

Event images never pass through our API. When an admin picks an image, the browser asks `POST /api/v1/admin/uploads/sas-url` for an upload slot and then PUTs the file straight to Azure Blob Storage. The API stays stateless: it does no multipart parsing, writes no temp files and doesn't stream uploads through its memory.

The upload URL is a SAS (shared access signature) URL, limited on purpose:

- **Write-only.** It can create and write that one blob, but it can't read, list or delete anything.
- **Short-lived.** It stops working after 5 minutes.
- **Named by us.** The blob name is a fresh GUID plus an extension for the content type, so a client can't choose a name that overwrites another image. We accept only JPEG, PNG, WebP and GIF.

The images container allows anonymous reads of single blobs, so the stored `imageUrl` works in an `<img>` tag. Listing the container is still refused.

A SAS URL can't cap the file size, so the browser rejects images over 5 MB before asking for a slot. An admin calling the API directly could upload something bigger.

Locally we run [Azurite](https://github.com/Azure/Azurite), Microsoft's Blob Storage emulator, without Docker:

```bash
npm install -g azurite@3.37.0
azurite-blob --silent --location ~/.azurite --skipApiVersionCheck &
dotnet user-secrets set "AzureBlob:ConnectionString" "UseDevelopmentStorage=true" --project EventTicketing.Api
```

On startup the API creates the container. In Development it also adds a CORS rule so the Vite dev server's origin may PUT to the emulator (`AzureBlob:CorsOrigins`). Integration tests and CI use a real Azurite too, with a throwaway container per test host.

## Tracing: one trace from the request to the consumer

We use OpenTelemetry to trace each request: the ASP.NET Core request, its PostgreSQL commands (Npgsql) and its outgoing HTTP calls (Stripe, Azure Blob Storage). Health checks aren't traced.

A booking's trace doesn't stop when the response goes out. The confirmation goes through the outbox, so the publish happens seconds later on a background worker, with no request around it. To keep it in the same trace:

1. `BookingService` saves the request's W3C `traceparent` on the outbox row (`outbox_messages.trace_parent`), in the same transaction as the order.
2. `OutboxWorker` starts its "outbox publish" span with that saved context as its parent.
3. RabbitMQ.Client (7.x) traces the publish itself and writes the `traceparent` into the AMQP message headers. On the consumer side it starts the "deliver" span in the same trace, linked to the publish.

So in Jaeger one booking shows up as one trace: the request and its SQL, then the outbox publish, the RabbitMQ publish and the consumer's delivery.

Logs and traces point at each other. Every log line written inside a span carries its trace and span ids (`@tr` and `@sp` in the compact JSON), and the request span carries the `correlation_id` tag.

Traces are exported over OTLP only when `OpenTelemetry:OtlpEndpoint` is set. Development sets it to `http://localhost:4317`. Locally we run Jaeger's single binary, without Docker:

```bash
mkdir -p ~/.local/opt ~/.local/bin
curl -sSL https://github.com/jaegertracing/jaeger/releases/download/v2.21.0/jaeger-2.21.0-linux-amd64.tar.gz | tar xz -C ~/.local/opt
ln -sf ~/.local/opt/jaeger-2.21.0-linux-amd64/jaeger ~/.local/bin/jaeger
jaeger &   # OTLP on :4317/:4318, UI on http://localhost:16686
```

Jaeger keeps traces in memory, so they're gone when it stops. Tests and CI don't export anything. The tests check the trace ids instead, with an in-memory exporter or an `ActivityListener`.

## Resilience: retries and a circuit breaker around Stripe

Stripe sits in the middle of a booking: the seat is reserved, the card is charged, then the order is saved. If Stripe is slow or failing, the buyer shouldn't wait on timeouts, and we shouldn't pile more requests onto a service that's already struggling.

The Stripe SDK sends its requests through an `HttpClient` from `IHttpClientFactory` with a resilience handler (`Microsoft.Extensions.Http.Resilience`, which is Polly v8). From the outside in:

- **Total timeout, 30 seconds**, retries included.
- **Retry**: up to 2 more tries for transient failures (network errors, 5xx, 408, 429), with exponential backoff and jitter from 500 ms. Only requests that are safe to repeat are retried: GETs, and POSTs carrying an `Idempotency-Key`. Stripe returns the first result for a repeated key instead of charging again. Our charges send the booking's own key, and the SDK adds one to its other POSTs. A declined card is a 402, not a transient failure, so it's never retried. The SDK's own retries are turned off, so requests aren't retried at two layers.
- **Circuit breaker**: when at least half of 5 or more calls in 30 seconds fail, it opens for 30 seconds. While it's open, calls fail at once without reaching Stripe. After the break, one trial call decides whether it closes again.
- **Per-attempt timeout, 10 seconds.**

While the breaker is open, a paid booking puts the seat back and answers **503 `payments_unavailable`** with `Retry-After: 30`. Saving a card answers the same way. The booking modal and the profile page tell the buyer how long to wait. Free events don't call Stripe, so they keep booking.

**Why not Blob Storage too.** Image uploads go from the browser straight to Azure with a SAS URL, so the API doesn't sit in that path. Creating the SAS is a local signature, not a network call. The API's only Blob calls are the container and CORS setup at startup, and the Azure SDK already retries those. A breaker there would protect nothing.

## Secrets: user-secrets locally, Key Vault in Azure

No secret is ever in `appsettings*.json` or in the repository. The app needs two: the JWT signing key (`Jwt:SigningKey`, at least 32 bytes) and the Stripe secret key (`Stripe:SecretKey`). Each environment supplies them its own way:

- **Locally**: `dotnet user-secrets`, which keeps them in your user profile, outside the repo:

  ```bash
  dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project EventTicketing.Api
  dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project EventTicketing.Api
  ```

- **CI**: each run generates a throwaway signing key with `openssl` into `Jwt__SigningKey`, and uses fake payments, so it needs no Stripe key at all.
- **Azure**: a persistent Key Vault, read at startup through `Azure.Extensions.AspNetCore.Configuration.Secrets`. The app adds the vault only when `KeyVault:Uri` is set (the `KeyVault__Uri` environment variable on the VM). It signs in with `DefaultAzureCredential`, which on the VM means its user-assigned managed identity (`AZURE_CLIENT_ID` names it). That identity has only the `Key Vault Secrets User` role, so it can read secrets and nothing else. No password or connection string for the vault exists anywhere.

The vault is shared by our three demo apps, so every secret name starts with the app's prefix, and the app loads only its own. Key Vault names can't contain `:`, so `--` stands for it:

| Secret name in the vault | Configuration key |
|---|---|
| `EventTicketing--Jwt--SigningKey` | `Jwt:SigningKey` |
| `EventTicketing--Stripe--SecretKey` | `Stripe:SecretKey` |

The app checks both keys on startup (`ValidateOnStart`), so a missing or too-short secret stops it from starting at all, instead of failing on the first sign-in or payment.

## Load test: no oversell under contention

The integration tests prove that 10 concurrent buyers can't oversell 1 seat. `loadtest/booking-contention.js` does the same at scale with [k6](https://k6.io): many virtual users race to book a small event over real HTTP, through the rate limiter, idempotency filter, payment step and outbox.

1. **Setup** signs in as the demo admin and creates a paid event with `SEATS` seats (default 50).
2. `BUYERS` bookings (default 500) run across `VUS` concurrent virtual users (default 100), each with its own idempotency key.
3. **Teardown** counts the orders the database really holds (through the admin orders API, not just the 201s we saw), checks that no seats are left, and deletes the event (its orders go with it).

The run fails unless exactly `SEATS` bookings succeed, every other one gets `409 sold_out`, nothing else comes back, and the order count equals the seat count.

It needs the API running with fake payments and the rate limits raised, since every virtual user comes from one IP:

```bash
cd EventTicketing.Api
Payments__Provider=Fake RateLimiting__GlobalPermitLimit=100000 RateLimiting__BookingPermitLimit=100000 dotnet run
# in another terminal, from the repo root (k6 is pinned in .tool-versions):
k6 run loadtest/booking-contention.js
k6 run -e SEATS=10 -e BUYERS=1000 -e VUS=200 -e BASE_URL=http://localhost:5017 loadtest/booking-contention.js
```

On a laptop, with a Debug build and SQL logging on, 500 buyers for 50 seats gave exactly 50 orders and 450 sold-out answers, at about 54 bookings a second (p95 latency 2.7 s). Every buyer updates the same event row, so the conditional `UPDATE` makes them queue for its lock. That queue is the point: it's what stops two buyers from getting the last seat.

## Azure demo deployment: one shared demo, started on demand

The demo isn't meant to run around the clock. Our portfolio site asks for it when a visitor opens the page, and it keeps running until nobody has opened the page for 30 minutes. Then everything is deleted, so nothing costs money between visits. Managed PostgreSQL, Redis and App Service take minutes to provision and bill hourly minimums, so the demo runs everything on **one Ubuntu VM** instead, the same way we run it locally:

- **`infra/demo-stack.bicep`** creates the VM (`Standard_B2s`), its network, a public IP with a DNS label, a network security group that opens only 80 and 443 (no SSH), and a storage account for event images. It attaches a **pre-created** user-assigned managed identity, so deploying a demo never needs permission to grant roles.
- **`infra/cloud-init.yaml`** installs PostgreSQL, Redis, RabbitMQ and Caddy from Ubuntu's packages, downloads the release build, and starts the API as a systemd service. Caddy gets a TLS certificate for `<label>.<region>.cloudapp.azure.com` and proxies to the API.
- **`.github/workflows/release.yml`** builds a release on every `v*` tag: the API published self-contained for linux-x64 with the built React app in its `wwwroot` (the API serves the app itself, from the same origin, so cookies and SignalR need no CORS), the compiled demo template, and the launcher below.
- **`EventTicketing.DemoLauncher`** is a small Azure Function that starts and stops the demo. **`infra/launcher.bicep`** deploys it once, with everything around it.

Behind Caddy every request arrives from 127.0.0.1. The API therefore takes the client's address and scheme from the `X-Forwarded-For` and `X-Forwarded-Proto` headers, but only when the request comes from a loopback proxy. That keeps per-IP rate limiting meaningful, and a client can't dodge a limit by sending the header itself.

### How the launcher works

There is never more than one demo, and every visitor shares it:

- **`POST /api/demo`**: the portfolio page calls this when it opens. The demo's end moves to 30 minutes from now; if no demo is running, the launcher starts one. The answer is `{ "state": ..., "url": ..., "expiresAt": ... }`.
- **`GET /api/demo`**: the page polls this until `state` is `ready`, which means `https://<demo>/health/ready` answers. Until then it's `starting` (or `stopping`, if a visitor arrives just as the previous demo is being deleted).
- **A timer every 2 minutes** deletes the demo once its end has passed. If a visitor asked for it while it was being deleted, the timer starts it again afterwards.

The demo is a **deployment stack** in its own resource group, `rg-eventticketing-demos`. The stack records every resource the template created, so deleting it removes exactly the demo. The only state the launcher keeps is the demo's end time, in a table in its own storage account.

### Why a stolen credential can't run up a bill

- **There is no key to steal.** The portfolio is a static site, so it holds nothing secret. The launcher signs in to Azure with its **managed identity**, which only code running inside that Function App can use; its storage account doesn't even accept keys.
- **The launcher can only touch the demos resource group.** Its custom role, *Event Ticketing demo deployer*, allows the resource types `demo-stack.bicep` creates, in `rg-eventticketing-demos` only, and nothing under `Microsoft.Authorization`: it can't grant itself more or remove the policies below. Elsewhere it may only attach the demo VMs' identity (*Managed Identity Operator* on that one identity) and read and write its own storage.
- **Azure Policy refuses everything else in the demos group, whoever asks:** VM size `Standard_B2s` only, `eastus` only, the demo's resource types only, and `Standard_LRS` storage only.
- **The public endpoint can only start this one demo.** Calling it in a loop keeps one small VM running: about $1.20 a day at worst.

### One-time setup

A long-lived resource group holds the vault and the demo VMs' identity (`<vault>` must be globally unique). The vault uses Azure RBAC, under which nobody can read or write secrets without a role, not even its creator, so you give yourself *Key Vault Secrets Officer* before adding them:

```bash
az group create -n rg-eventticketing-shared -l eastus
az keyvault create -n <vault> -g rg-eventticketing-shared --enable-rbac-authorization true
az role assignment create --role "Key Vault Secrets Officer" \
  --assignee "$(az ad signed-in-user show --query id -o tsv)" \
  --scope "$(az keyvault show -n <vault> --query id -o tsv)"
az identity create -n id-eventticketing-demo -g rg-eventticketing-shared
az role assignment create --role "Key Vault Secrets User" \
  --assignee-object-id "$(az identity show -n id-eventticketing-demo -g rg-eventticketing-shared --query principalId -o tsv)" \
  --assignee-principal-type ServicePrincipal \
  --scope "$(az keyvault show -n <vault> --query id -o tsv)"
az keyvault secret set --vault-name <vault> -n EventTicketing--Jwt--SigningKey --value "$(openssl rand -base64 48)"
az keyvault secret set --vault-name <vault> -n EventTicketing--Stripe--SecretKey --value "sk_test_..."
```

Then, with a release published (push a `v*` tag; the repository must be public, because the VM and the launcher download it without a token), deploy the launcher as the subscription's Owner and upload its code:

```bash
az deployment sub create -l eastus -f infra/launcher.bicep -p \
  allowedOrigins='["https://<portfolio-site>"]' \
  githubRepository=<owner>/<repo> \
  keyVaultName=<vault> \
  adminSshPublicKey="$(cat ~/.ssh/id_ed25519.pub)" \
  --query properties.outputs
curl -fsSLO https://github.com/<owner>/<repo>/releases/latest/download/demo-launcher.zip
az functionapp deployment source config-zip -g rg-eventticketing-shared -n <functionAppName> --src demo-launcher.zip
```

The deployment prints `launcherUrl` and `functionAppName`. New releases of the app need no redeploy: each demo downloads the latest release. Run the commands again only when the launcher itself changes.

### Calling it from the portfolio page

```js
const launcher = 'https://<function-app>.azurewebsites.net/api/demo';

async function showDemo(element) {
  let status = await (await fetch(launcher, { method: 'POST' })).json();
  while (status.state !== 'ready') {
    element.textContent = 'Starting the live demo (about 5 minutes)...';
    await new Promise((resolve) => setTimeout(resolve, 10000));
    status = await (await fetch(launcher)).json();
  }
  element.innerHTML = `<a href="${status.url}">Open the live demo</a>`;
}
```

### Looking inside

The launcher logs to Application Insights (`appi-demo-launcher` in the shared group). For the VM, without SSH: `az vm run-command invoke -g rg-eventticketing-demos -n vm-event-ticketing --command-id RunShellScript --scripts "journalctl -u eventticketing -n 50"`. To end a demo early: `az stack group delete -n event-ticketing-demo -g rg-eventticketing-demos --action-on-unmanage deleteAll --yes`.

Known trade-offs:

- **Shared data.** Visitors share the demo's data, including the admin account, so one visitor can change events another is looking at. Every new demo starts again from the seeded data.
- **Storage key in `customData`.** The storage account key reaches the VM through its `customData`, which anyone with read access to the VM can see. That's acceptable for a demo whose storage account dies with it.
- **Repeated certificates.** The demo always has the same address, and Let's Encrypt issues only a handful of certificates per week for one name. After that, Caddy falls back to ZeroSSL on its own.
- **Package versions.** Ubuntu's packages bring PostgreSQL 16 and RabbitMQ 3.12 rather than the versions we pin locally; the app doesn't depend on anything newer.

## Demo dates that stay upcoming

The seeder creates 30 demo events starting tomorrow, but only on an empty database. Each new Azure demo gets a fresh one, so its events are always upcoming. A database that lives for weeks, like a local dev one, would slowly fill with past events instead.

So the API runs a small background job, `DemoDateRefreshWorker`, when it starts and once a day after that. If the earliest demo event has already started, `DemoDateRefresher` moves all 30 so the demo begins tomorrow again, with the seeder's spacing and times of day (New York wall-clock times). It recognises demo events by their seeded titles, so events an admin created are never moved. Orders stay with their events and simply follow the new dates. Cached list pages are evicted so the new dates show at once.

The old app did this with a separate console program run by cron. Running it inside the API needs no cron entry and can't be forgotten, and the dates only matter while the API is running anyway.
