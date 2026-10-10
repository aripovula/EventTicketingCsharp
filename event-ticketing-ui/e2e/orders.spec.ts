import { expect, test } from '@playwright/test'

test('My orders asks a signed-out visitor to sign in', async ({ page }) => {
  await page.goto('/')

  await page.getByRole('banner').getByRole('link', { name: 'My orders' }).click()

  await expect(page).toHaveURL('/orders')
  await expect(page.getByRole('heading', { name: 'My orders' })).toBeVisible()
  await expect(page.getByRole('main').getByRole('link', { name: 'Sign in' })).toBeVisible()
})

test('a signed-in user sees their orders', async ({ page }) => {
  await page.route('**/api/v1/auth/me', route =>
    route.fulfill({ json: { data: { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }, errors: [] } }))
  await page.route('**/api/v1/auth/me/orders**', route => route.fulfill({ json: { data: [
    { id: 42, eventId: 1, eventTitle: 'Jazz Night', eventVenue: 'Blue Note Club', eventStartTime: '2026-11-02T23:00:00Z',
      eventEndTime: '2026-11-03T02:00:00Z', email: 'john@example.com', priceCents: 2500, bookedAt: '2026-10-09T12:00:00Z' },
  ], errors: [], meta: { nextCursor: null } } }))
  await page.goto('/orders')

  const order = page.getByRole('list', { name: 'orders' }).getByRole('listitem')
  await expect(order).toContainText('Jazz Night')
  await expect(order).toContainText('Order #42 · $25.00')
})

test('a signed-in user opens My Calendar from the header', async ({ page }) => {
  await page.route('**/api/v1/auth/me', route =>
    route.fulfill({ json: { data: { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }, errors: [] } }))
  await page.route('**/api/v1/auth/me/orders**', route => route.fulfill({ json: { data: [], errors: [], meta: { nextCursor: null } } }))
  await page.goto('/')

  await page.getByRole('banner').getByRole('link', { name: 'My Calendar' }).click()

  await expect(page).toHaveURL('/calendar')
  await expect(page.getByRole('heading', { name: 'My Calendar' })).toBeVisible()
})

test('My Calendar shows booked events on their day', async ({ page }) => {
  const start = new Date()
  start.setHours(19, 0, 0, 0)
  await page.route('**/api/v1/auth/me', route =>
    route.fulfill({ json: { data: { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }, errors: [] } }))
  await page.route('**/api/v1/auth/me/orders**', route => route.fulfill({ json: { data: [
    { id: 42, eventId: 1, eventTitle: 'Booked Gig', eventVenue: 'Hall', eventType: 'Music', eventStartTime: start.toISOString(),
      eventEndTime: new Date(start.getTime() + 3600000).toISOString(), email: 'john@example.com', priceCents: 2500, bookedAt: '2026-10-09T12:00:00Z' },
  ], errors: [], meta: { nextCursor: null } } }))
  await page.goto('/calendar')

  await expect(page.getByText('Booked Gig')).toBeVisible()
})
