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
  let posted: unknown
  await page.route('**/api/v1/events/1/bookings', route => {
    booked = true
    posted = route.request().postDataJSON()
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
  // No Stripe key in E2E runs, so the form pays with Stripe's approved test card.
  expect(posted).toEqual({ email: 'guest@example.com', paymentMethodId: 'pm_card_visa' })
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

test('a retried booking reuses its idempotency key', async ({ page }) => {
  await page.route('**/api/v1/events/1', route => route.fulfill({ json: { data: event, errors: [] } }))
  const keys: string[] = []
  await page.route('**/api/v1/events/1/bookings', route => {
    keys.push(route.request().headers()['idempotency-key'])
    return route.fulfill({ status: 500, body: 'oops' })
  })
  await page.goto('/events/1')

  await page.getByRole('button', { name: 'Buy ticket' }).click()
  await page.getByRole('dialog').getByLabel('Email').fill('guest@example.com')
  await page.getByRole('button', { name: 'Place order' }).click()
  await expect(page.getByRole('dialog').getByRole('alert')).toBeVisible()
  await page.getByRole('button', { name: 'Place order' }).click()

  await expect.poll(() => keys.length).toBe(2)
  expect(keys[0]).toMatch(/^[0-9a-f-]{36}$/)
  expect(keys[1]).toBe(keys[0])
})

test('a declined card is explained, and another card can be tried', async ({ page }) => {
  await page.route('**/api/v1/events/1', route => route.fulfill({ json: { data: event, errors: [] } }))
  const attempts: { key: string, card: string }[] = []
  await page.route('**/api/v1/events/1/bookings', route => {
    attempts.push({ key: route.request().headers()['idempotency-key'], card: route.request().postDataJSON().paymentMethodId })
    return attempts.length === 1
      ? route.fulfill({ status: 402, json: { data: null, errors: [{ code: 'payment_declined', detail: 'Your card was declined.' }] } })
      : route.fulfill({ status: 500, body: 'oops' })
  })
  await page.goto('/events/1')

  await page.getByRole('button', { name: 'Buy ticket' }).click()
  const dialog = page.getByRole('dialog')
  await dialog.getByLabel('Email').fill('guest@example.com')
  await dialog.getByLabel('Visa 4000 0000 0000 0002 (declined)').check()
  await dialog.getByRole('button', { name: 'Place order' }).click()
  await expect(dialog.getByRole('alert')).toHaveText('Your card was declined.')
  await dialog.getByLabel('Visa 4242 4242 4242 4242 (approved)').check()
  await dialog.getByRole('button', { name: 'Place order' }).click()

  await expect.poll(() => attempts.length).toBe(2)
  expect(attempts.map(a => a.card)).toEqual(['pm_card_chargeDeclined', 'pm_card_visa'])
  expect(attempts[1].key).not.toBe(attempts[0].key)
})
