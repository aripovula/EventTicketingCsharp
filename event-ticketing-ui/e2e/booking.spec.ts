import { expect, test } from '@playwright/test'

const event = {
  id: 1, title: 'Jazz Night', description: 'Live jazz.', startTime: '2026-11-02T23:00:00Z', endTime: '2026-11-03T02:00:00Z',
  venue: 'Blue Note Club', eventType: 'Music', totalSeats: 120, availableSeats: 48, priceCents: 2500, imageUrl: null,
}

test('a guest books a seat and lands on the order confirmation', async ({ page }) => {
  let booked = false
  await page.route('**/api/v1/events/1', route =>
    route.fulfill({ json: { data: { ...event, availableSeats: booked ? 47 : 48 }, errors: [] } }))
  const order = { id: 42, eventId: 1, eventTitle: 'Jazz Night', eventVenue: 'Blue Note Club', eventStartTime: '2026-11-02T23:00:00Z', eventEndTime: '2026-11-03T02:00:00Z', email: 'guest@example.com', priceCents: 2500, bookedAt: '2026-10-09T12:00:00Z' }
  await page.route('**/api/v1/events/1/bookings', route => {
    booked = true
    return route.fulfill({ status: 201, json: { data: { ...order, guestAccessToken: 'secret' }, errors: [] } })
  })
  await page.route('**/api/v1/orders/42', route =>
    route.request().headers()['x-order-token'] === 'secret'
      ? route.fulfill({ json: { data: order, errors: [] } })
      : route.fulfill({ status: 404, json: { data: null, errors: [] } }))
  await page.goto('/events/1')

  await page.getByRole('button', { name: 'Buy ticket' }).click()
  await page.getByRole('dialog').getByLabel('Email').fill('guest@example.com')
  await page.getByRole('button', { name: 'Place order' }).click()

  await expect(page).toHaveURL('/orders/42?token=secret')
  await expect(page.getByRole('heading', { name: "You're going!" })).toBeVisible()
  await expect(page.getByText(/Order #42/)).toContainText('$25.00')
})

test('a sold-out answer is shown in the booking modal', async ({ page }) => {
  await page.route('**/api/v1/events/1', route => route.fulfill({ json: { data: event, errors: [] } }))
  await page.route('**/api/v1/events/1/bookings', route =>
    route.fulfill({ status: 409, json: { data: null, errors: [{ code: 'sold_out', detail: 'Sorry, this event just sold out.' }] } }))
  await page.goto('/events/1')

  await page.getByRole('button', { name: 'Buy ticket' }).click()
  await page.getByRole('dialog').getByLabel('Email').fill('guest@example.com')
  await page.getByRole('button', { name: 'Place order' }).click()

  await expect(page.getByRole('dialog').getByRole('alert')).toHaveText('Sorry, this event just sold out.')
})

test('booking too often asks the visitor to wait', async ({ page }) => {
  await page.route('**/api/v1/events/1', route => route.fulfill({ json: { data: event, errors: [] } }))
  await page.route('**/api/v1/events/1/bookings', route => route.fulfill({
    status: 429, headers: { 'Retry-After': '30' }, json: { data: null, errors: [{ code: 'rate_limited' }] },
  }))
  await page.goto('/events/1')

  await page.getByRole('button', { name: 'Buy ticket' }).click()
  await page.getByRole('dialog').getByLabel('Email').fill('guest@example.com')
  await page.getByRole('button', { name: 'Place order' }).click()

  await expect(page.getByRole('dialog').getByRole('alert')).toHaveText('Too many booking attempts. Please try again in 30 seconds.')
})
