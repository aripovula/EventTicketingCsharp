import { expect, test } from '@playwright/test'

const event = {
  id: 1, title: 'Jazz Night', description: 'Live jazz.', startTime: '2026-11-02T23:00:00Z', endTime: '2026-11-03T02:00:00Z',
  venue: 'Blue Note Club', eventType: 'Music', totalSeats: 120, availableSeats: 48, priceCents: 2500, imageUrl: null,
}

test('a guest books a seat from the event page', async ({ page }) => {
  let booked = false
  await page.route('**/api/v1/events/1', route =>
    route.fulfill({ json: { data: { ...event, availableSeats: booked ? 47 : 48 }, errors: [] } }))
  await page.route('**/api/v1/events/1/bookings', route => {
    booked = true
    return route.fulfill({ status: 201, json: { data: { id: 42, eventId: 1, eventTitle: 'Jazz Night', email: 'guest@example.com', priceCents: 2500, bookedAt: '2026-10-09T12:00:00Z' }, errors: [] } })
  })
  await page.goto('/events/1')

  await page.getByRole('button', { name: 'Buy ticket' }).click()
  await page.getByRole('dialog').getByLabel('Email').fill('guest@example.com')
  await page.getByRole('button', { name: 'Place order' }).click()

  await expect(page.getByRole('status')).toHaveText('Booked! Your order number is #42.')
  await expect(page.getByText('47 of 120 seats available')).toBeVisible()
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
