// Many buyers race for the last seats of one event: exactly SEATS bookings must succeed, the rest
// must get 409 sold_out, and nothing may be oversold. See the README's "Load test" section.
import http from 'k6/http'
import { check } from 'k6'
import { Counter } from 'k6/metrics'

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5017'
const SEATS = Number(__ENV.SEATS || 50)
const BUYERS = Number(__ENV.BUYERS || 500)
const VUS = Number(__ENV.VUS || 100)

const booked = new Counter('bookings_succeeded')
const soldOut = new Counter('bookings_sold_out')
const unexpected = new Counter('bookings_unexpected')
const oversold = new Counter('seats_oversold')

export const options = {
  scenarios: {
    contention: { executor: 'shared-iterations', vus: VUS, iterations: BUYERS, maxDuration: '2m' },
  },
  // A sold-out answer is the expected outcome for most buyers, not a failed request.
  thresholds: {
    checks: ['rate==1'],
    bookings_succeeded: [`count==${SEATS}`],
    bookings_unexpected: ['count==0'],
    seats_oversold: ['count==0'],
  },
}

const json = { 'Content-Type': 'application/json' }

function adminHeaders(token) {
  return { ...json, Authorization: `Bearer ${token}` }
}

export function setup() {
  const login = http.post(`${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({ email: 'admin@example.com', password: 'Password' }), { headers: json })
  const token = login.cookies.access_token?.[0]?.value
  if (!token) throw new Error(`Admin sign-in failed (${login.status}): is the API running at ${BASE_URL}?`)

  const start = new Date(Date.now() + 7 * 24 * 3600 * 1000)
  const created = http.post(`${BASE_URL}/api/v1/events`, JSON.stringify({
    title: `Load test ${start.getTime()}`,
    description: 'Created by loadtest/booking-contention.js; deleted when the run ends.',
    startTime: start.toISOString(),
    endTime: new Date(start.getTime() + 2 * 3600 * 1000).toISOString(),
    venue: 'k6 Arena',
    eventType: 'Other',
    totalSeats: SEATS,
    availableSeats: SEATS,
    priceCents: 2500,
  }), { headers: adminHeaders(token) })
  if (created.status !== 201) throw new Error(`Creating the event failed (${created.status}): ${created.body}`)
  return { token, eventId: created.json('data.id') }
}

export default function ({ eventId }) {
  const res = http.post(`${BASE_URL}/api/v1/events/${eventId}/bookings`,
    JSON.stringify({ email: `buyer-${__VU}-${__ITER}@example.com`, paymentMethodId: 'pm_card_visa' }),
    {
      headers: { ...json, 'Idempotency-Key': `k6-${eventId}-${__VU}-${__ITER}` },
      tags: { name: 'book' },
      responseCallback: http.expectedStatuses(201, 409),
    })

  if (res.status === 201) booked.add(1)
  else if (res.status === 409) soldOut.add(1)
  else unexpected.add(1)
  check(res, { 'booked or sold out': r => r.status === 201 || r.status === 409 })
}

export function teardown({ token, eventId }) {
  // Count the orders the database really holds, not just the 201s we saw.
  let orders = 0
  let cursor = ''
  do {
    const page = http.get(`${BASE_URL}/api/v1/admin/orders?eventId=${eventId}&limit=50${cursor ? `&cursor=${encodeURIComponent(cursor)}` : ''}`,
      { headers: adminHeaders(token) })
    orders += page.json('data').length
    cursor = page.json('meta.nextCursor')
  } while (cursor)

  const event = http.get(`${BASE_URL}/api/v1/events/${eventId}`).json('data')
  check(null, {
    'orders == seats': () => orders === SEATS,
    'no seats left': () => event.availableSeats === 0,
  })
  if (orders > SEATS) oversold.add(orders - SEATS)
  console.log(`${orders} orders for ${SEATS} seats; ${event.availableSeats} seats left`)

  http.del(`${BASE_URL}/api/v1/events/${eventId}`, null, { headers: adminHeaders(token) })
}
