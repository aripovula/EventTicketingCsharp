import { expect, test, type Page } from '@playwright/test'
import { fakeHub } from './fakeHub'

async function signedInAs(page: Page, role: 'admin' | 'user') {
  await page.route('**/api/v1/auth/me', route =>
    route.fulfill({ json: { data: { userId: 4, name: role === 'admin' ? 'Admin' : 'John Doe', email: `${role}@example.com`, role }, errors: [] } }),
  )
  await page.route('**/api/v1/events?**', route => route.fulfill({ json: { data: [], errors: [], meta: { nextCursor: null } } }))
}

test('an admin reaches the admin page from the header', async ({ page }) => {
  await signedInAs(page, 'admin')
  await page.goto('/')

  await page.getByRole('banner').getByRole('link', { name: 'Events', exact: true }).click()

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

// A 1×1 transparent PNG.
const PNG = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=', 'base64')

test('an admin uploads an event image straight to blob storage', async ({ page }) => {
  await signedInAs(page, 'admin')
  // Same-origin stand-in for the storage account, so the test needs no CORS.
  const imageUrl = '/fake-blob/images/abc.png'
  let sasRequest: unknown
  await page.route('**/api/v1/admin/uploads/sas-url', route => {
    sasRequest = route.request().postDataJSON()
    return route.fulfill({ json: { data: { uploadUrl: `${imageUrl}?sig=write-only`, imageUrl, expiresAt: '2026-10-12T20:05:00Z' }, errors: [] } })
  })
  let uploaded: { blobType?: string, body?: Buffer | null } = {}
  await page.route('**/fake-blob/images/abc.png**', route => {
    if (route.request().method() === 'PUT') {
      uploaded = { blobType: route.request().headers()['x-ms-blob-type'], body: route.request().postDataBuffer() }
      return route.fulfill({ status: 201 })
    }
    return route.fulfill({ contentType: 'image/png', body: PNG })
  })
  let posted: Record<string, unknown> | null = null
  await page.route('**/api/v1/events', route => {
    posted = route.request().postDataJSON()
    return route.fulfill({ status: 201, json: { data: { id: 99, ...posted }, errors: [] } })
  })
  await page.goto('/admin/new')

  await page.getByLabel('Image').setInputFiles({ name: 'poster.png', mimeType: 'image/png', buffer: PNG })

  await expect(page.getByRole('img', { name: 'Event image preview' })).toBeVisible()
  expect(sasRequest).toEqual({ contentType: 'image/png' })
  expect(uploaded.blobType).toBe('BlockBlob')
  expect(uploaded.body).toEqual(PNG)
  await page.getByLabel('Title').fill('Poster Gig')
  await page.getByLabel('Venue').fill('Main Hall')
  await page.getByLabel('Description').fill('A gig with a poster.')
  await page.getByLabel('Start time').fill('2026-11-02T19:00')
  await page.getByLabel('End time').fill('2026-11-02T22:00')
  await page.getByLabel('Total seats').fill('100')
  await page.getByLabel('Available seats').fill('100')
  await page.getByLabel('Price ($)').fill('19.99')
  await page.getByRole('button', { name: 'Create event' }).click()

  await expect(page).toHaveURL('/admin')
  expect(posted).toMatchObject({ title: 'Poster Gig', imageUrl })
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

test('an admin opens the events calendar from the header', async ({ page }) => {
  await signedInAs(page, 'admin')
  await page.goto('/admin')

  await page.getByRole('banner').getByRole('link', { name: 'Events Calendar' }).click()

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

test('a calendar popup that opens below the window can be scrolled to', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 300 })
  await signedInAs(page, 'admin')
  const start = new Date()
  start.setHours(19, 0, 0, 0)
  const event = { id: 10, title: 'Low Gig', description: '', startTime: start.toISOString(), endTime: new Date(start.getTime() + 3600000).toISOString(),
    venue: 'Low Hall', eventType: 'Music', totalSeats: 10, availableSeats: 2, priceCents: 500, imageUrl: null }
  await page.route('**/api/v1/events?**', route => route.fulfill({ json: { data: [event], errors: [], meta: { nextCursor: null } } }))
  await page.route('**/api/v1/events/10', route => route.fulfill({ json: { data: event, errors: [] } }))
  await page.goto('/admin/calendar')
  await page.getByText('Low Gig – 2').click()

  await page.getByRole('dialog', { name: 'Low Gig' }).getByRole('link', { name: 'Edit' }).click()

  await expect(page).toHaveURL('/admin/10/edit')
  await expect(page.getByLabel('Title')).toHaveValue('Low Gig')
})

test('an admin opens all orders from the admin page', async ({ page }) => {
  await signedInAs(page, 'admin')
  await page.route('**/api/v1/admin/orders', route => route.fulfill({ json: { data: [
    { id: 42, eventId: 1, eventTitle: 'Jazz Night', eventVenue: 'Blue Note Club', eventType: 'Music', eventStartTime: '2026-11-02T23:00:00Z',
      eventEndTime: '2026-11-03T02:00:00Z', email: 'jane@example.com', priceCents: 2500, bookedAt: '2026-10-09T12:00:00Z' },
  ], errors: [], meta: { nextCursor: null } } }))
  await page.goto('/admin')

  await page.getByRole('link', { name: 'All orders' }).click()

  await expect(page.getByRole('heading', { name: 'All orders' })).toBeVisible()
  await expect(page.getByRole('row').nth(1)).toContainText('jane@example.com')
})

test('an admin opens the seat summary from the admin page', async ({ page }) => {
  await signedInAs(page, 'admin')
  await page.route('**/api/v1/admin/summary', route => route.fulfill({ json: { data: [
    { eventId: 1, title: 'Jazz Night', totalSeats: 120, soldSeats: 72, remainingSeats: 48, revenueCents: 180000 },
  ], errors: [] } }))
  await page.goto('/admin')

  await page.getByRole('link', { name: 'Seat summary' }).click()

  await expect(page.getByRole('heading', { name: 'Seat summary' })).toBeVisible()
  await expect(page.getByRole('row').nth(1)).toContainText('$1,800.00')
})

test('the seat summary recounts live when a booking is made', async ({ page }) => {
  await signedInAs(page, 'admin')
  const hub = await fakeHub(page)
  let sold = 72
  await page.route('**/api/v1/admin/summary', route => route.fulfill({ json: { data: [
    { eventId: 1, title: 'Jazz Night', totalSeats: 120, soldSeats: sold, remainingSeats: 120 - sold, revenueCents: sold * 2500 },
  ], errors: [] } }))
  await page.goto('/admin/summary')
  await expect(page.getByRole('row').nth(1)).toContainText('$1,800.00')

  sold = 73
  await hub.push('BookingMade', 1)

  await expect(page.getByRole('row').nth(1)).toContainText('$1,825.00')
})

test('an admin filters all orders by event', async ({ page }) => {
  await signedInAs(page, 'admin')
  const order = { eventId: 1, eventVenue: 'Hall', eventType: 'Music', eventStartTime: '2026-11-02T23:00:00Z',
    eventEndTime: '2026-11-03T02:00:00Z', email: 'jane@example.com', priceCents: 2500, bookedAt: '2026-10-09T12:00:00Z' }
  const event = { description: '', startTime: '2026-11-02T23:00:00Z', endTime: '2026-11-03T02:00:00Z', venue: 'Hall',
    eventType: 'Music', totalSeats: 10, availableSeats: 5, priceCents: 2500, imageUrl: null }
  await page.route('**/api/v1/events?**', route => route.fulfill({ json: { data: [
    { ...event, id: 1, title: 'Jazz Night' }, { ...event, id: 7, title: 'Comedy Showcase' },
  ], errors: [], meta: { nextCursor: null } } }))
  await page.route('**/api/v1/admin/orders', route => route.fulfill({ json: { data: [
    { ...order, id: 42, eventTitle: 'Jazz Night' }, { ...order, id: 43, eventId: 7, eventTitle: 'Comedy Showcase' },
  ], errors: [], meta: { nextCursor: null } } }))
  await page.route('**/api/v1/admin/orders?eventId=7', route => route.fulfill({ json: { data: [
    { ...order, id: 43, eventId: 7, eventTitle: 'Comedy Showcase' },
  ], errors: [], meta: { nextCursor: null } } }))
  await page.goto('/admin/orders')
  await expect(page.getByRole('row')).toHaveCount(3)

  await page.getByRole('combobox', { name: 'Filter by event' }).selectOption('Comedy Showcase')

  await expect(page.getByRole('row')).toHaveCount(2)
  await expect(page.getByRole('row').nth(1)).toContainText('#43')
})

test('all orders shows a new booking live', async ({ page }) => {
  await signedInAs(page, 'admin')
  const hub = await fakeHub(page)
  const order = { eventId: 1, eventTitle: 'Jazz Night', eventVenue: 'Hall', eventType: 'Music', eventStartTime: '2026-11-02T23:00:00Z',
    eventEndTime: '2026-11-03T02:00:00Z', email: 'jane@example.com', priceCents: 2500, bookedAt: '2026-10-09T12:00:00Z' }
  let orders = [{ ...order, id: 42 }]
  await page.route('**/api/v1/admin/orders', route => route.fulfill({ json: { data: orders, errors: [], meta: { nextCursor: null } } }))
  await page.goto('/admin/orders')
  await expect(page.getByRole('row')).toHaveCount(2)

  orders = [{ ...order, id: 43, email: 'new@example.com' }, ...orders]
  await hub.push('BookingMade', 1)

  await expect(page.getByRole('row')).toHaveCount(3)
  await expect(page.getByRole('row').nth(1)).toContainText('new@example.com')
})
