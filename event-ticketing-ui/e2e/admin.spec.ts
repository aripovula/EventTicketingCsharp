import { expect, test, type Page } from '@playwright/test'

async function signedInAs(page: Page, role: 'admin' | 'user') {
  await page.route('**/api/v1/auth/me', route =>
    route.fulfill({ json: { data: { userId: 4, name: role === 'admin' ? 'Admin' : 'John Doe', email: `${role}@example.com`, role }, errors: [] } }),
  )
  await page.route('**/api/v1/events?**', route => route.fulfill({ json: { data: [], errors: [], meta: { nextCursor: null } } }))
}

test('an admin reaches the admin page from the header', async ({ page }) => {
  await signedInAs(page, 'admin')
  await page.goto('/')

  await page.getByRole('banner').getByRole('link', { name: 'Admin' }).click()

  await expect(page).toHaveURL('/admin')
  await expect(page.getByRole('heading', { name: 'Admin' })).toBeVisible()
})

test('a regular user is told they lack permission', async ({ page }) => {
  await signedInAs(page, 'user')
  await page.goto('/admin')

  await expect(page.getByText('You do not have permission to view this page.')).toBeVisible()
})

test('an admin deletes an event after confirming', async ({ page }) => {
  await signedInAs(page, 'admin')
  const event = { id: 5, title: 'Doomed Gig', description: '', startTime: '2026-11-02T23:00:00Z', endTime: '2026-11-03T02:00:00Z',
    venue: 'Hall', eventType: 'Music', totalSeats: 10, availableSeats: 10, priceCents: 100, imageUrl: null }
  await page.route('**/api/v1/events?**', route => route.fulfill({ json: { data: [event], errors: [], meta: { nextCursor: null } } }))
  await page.route('**/api/v1/events/5', route => route.fulfill({ status: 204 }))
  await page.goto('/admin')

  await page.getByRole('button', { name: 'Delete Doomed Gig' }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Delete' }).click()

  await expect(page.getByRole('list', { name: 'admin events' }).getByRole('listitem')).toHaveCount(0)
})

test('an admin opens the new-event form from the admin page', async ({ page }) => {
  await signedInAs(page, 'admin')
  await page.goto('/admin')

  await page.getByRole('link', { name: '+ New event' }).click()

  await expect(page).toHaveURL('/admin/new')
  await expect(page.getByRole('heading', { name: 'New event' })).toBeVisible()
})

test('an admin creates an event and returns to the admin page', async ({ page }) => {
  await signedInAs(page, 'admin')
  let posted: Record<string, unknown> | null = null
  await page.route('**/api/v1/events', route => {
    posted = route.request().postDataJSON()
    return route.fulfill({ status: 201, json: { data: { id: 99, ...posted }, errors: [] } })
  })
  await page.goto('/admin/new')

  await page.getByLabel('Title').fill('Brand New Gig')
  await page.getByLabel('Venue').fill('Main Hall')
  await page.getByLabel('Description').fill('A new gig.')
  await page.getByLabel('Start time').fill('2026-11-02T19:00')
  await page.getByLabel('End time').fill('2026-11-02T22:00')
  await page.getByLabel('Total seats').fill('100')
  await page.getByLabel('Available seats').fill('100')
  await page.getByLabel('Price ($)').fill('19.99')
  await page.getByRole('button', { name: 'Create event' }).click()

  await expect(page).toHaveURL('/admin')
  expect(posted).toMatchObject({ title: 'Brand New Gig', priceCents: 1999 })
})
