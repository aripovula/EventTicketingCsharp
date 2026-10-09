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

test('an admin opens an event for editing from the admin list', async ({ page }) => {
  await signedInAs(page, 'admin')
  const event = { id: 5, title: 'Editable Gig', description: 'Edit me.', startTime: '2026-11-02T23:00:00Z', endTime: '2026-11-03T02:00:00Z',
    venue: 'Hall', eventType: 'Comedy', totalSeats: 10, availableSeats: 10, priceCents: 1999, imageUrl: null }
  await page.route('**/api/v1/events?**', route => route.fulfill({ json: { data: [event], errors: [], meta: { nextCursor: null } } }))
  await page.route('**/api/v1/events/5', route => route.fulfill({ json: { data: event, errors: [] } }))
  await page.goto('/admin')

  await page.getByRole('link', { name: 'Edit Editable Gig' }).click()

  await expect(page.getByRole('heading', { name: 'Edit event' })).toBeVisible()
  await expect(page.getByLabel('Title')).toHaveValue('Editable Gig')
  await expect(page.getByLabel('Event type')).toHaveValue('Comedy')
  await expect(page.getByLabel('Price ($)')).toHaveValue('19.99')
})

test('an admin saves changes to an event', async ({ page }) => {
  await signedInAs(page, 'admin')
  const event = { id: 6, title: 'Old Title', description: 'Edit me.', startTime: '2026-11-02T23:00:00Z', endTime: '2026-11-03T02:00:00Z',
    venue: 'Hall', eventType: 'Music', totalSeats: 10, availableSeats: 10, priceCents: 1000, imageUrl: null }
  let saved: Record<string, unknown> | null = null
  await page.route('**/api/v1/events/6', route => {
    if (route.request().method() === 'PUT') {
      saved = route.request().postDataJSON()
      return route.fulfill({ status: 204 })
    }
    return route.fulfill({ json: { data: event, errors: [] } })
  })
  await page.goto('/admin/6/edit')

  await page.getByLabel('Title').fill('New Title')
  await page.getByRole('button', { name: 'Save changes' }).click()

  await expect(page).toHaveURL('/admin')
  expect(saved).toMatchObject({ title: 'New Title', priceCents: 1000 })
})

test('an admin opens the events calendar from the admin page', async ({ page }) => {
  await signedInAs(page, 'admin')
  await page.goto('/admin')

  await page.getByRole('link', { name: 'Events Calendar' }).click()

  await expect(page).toHaveURL('/admin/calendar')
  await expect(page.getByRole('heading', { name: 'Events Calendar' })).toBeVisible()
})

test('the events calendar shows events as title and remaining seats', async ({ page }) => {
  await signedInAs(page, 'admin')
  const start = new Date()
  start.setHours(19, 0, 0, 0)
  const end = new Date(start.getTime() + 3 * 60 * 60 * 1000)
  const event = { id: 7, title: 'Calendar Gig', description: '', startTime: start.toISOString(), endTime: end.toISOString(),
    venue: 'Hall', eventType: 'Music', totalSeats: 10, availableSeats: 7, priceCents: 100, imageUrl: null }
  await page.route('**/api/v1/events?**', route => route.fulfill({ json: { data: [event], errors: [], meta: { nextCursor: null } } }))
  await page.goto('/admin/calendar')

  await expect(page.getByText('Calendar Gig – 7')).toBeVisible()
})

test('hovering a calendar event shows its remaining seats', async ({ page }) => {
  await signedInAs(page, 'admin')
  const start = new Date()
  start.setHours(19, 0, 0, 0)
  const event = { id: 8, title: 'Hover Gig', description: '', startTime: start.toISOString(), endTime: new Date(start.getTime() + 3600000).toISOString(),
    venue: 'Hall', eventType: 'Music', totalSeats: 10, availableSeats: 3, priceCents: 100, imageUrl: null }
  await page.route('**/api/v1/events?**', route => route.fulfill({ json: { data: [event], errors: [], meta: { nextCursor: null } } }))
  await page.goto('/admin/calendar')

  await page.getByText('Hover Gig – 3').hover()

  await expect(page.getByRole('tooltip')).toContainText('3 seats left')
})

test('clicking a calendar event opens details with a working edit link', async ({ page }) => {
  await signedInAs(page, 'admin')
  const start = new Date()
  start.setHours(19, 0, 0, 0)
  const event = { id: 9, title: 'Popup Gig', description: 'Click me.', startTime: start.toISOString(), endTime: new Date(start.getTime() + 3600000).toISOString(),
    venue: 'Popup Hall', eventType: 'Music', totalSeats: 10, availableSeats: 4, priceCents: 1999, imageUrl: null }
  await page.route('**/api/v1/events?**', route => route.fulfill({ json: { data: [event], errors: [], meta: { nextCursor: null } } }))
  await page.route('**/api/v1/events/9', route => route.fulfill({ json: { data: event, errors: [] } }))
  await page.goto('/admin/calendar')

  await page.getByText('Popup Gig – 4').click()
  const popup = page.getByRole('dialog', { name: 'Popup Gig' })
  await expect(popup).toContainText('Popup Hall')
  await expect(popup).toContainText('$19.99')
  await popup.getByRole('link', { name: 'Edit' }).click()

  await expect(page).toHaveURL('/admin/9/edit')
  await expect(page.getByLabel('Title')).toHaveValue('Popup Gig')
})
