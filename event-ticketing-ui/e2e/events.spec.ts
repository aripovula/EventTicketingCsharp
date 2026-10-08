import { expect, test } from '@playwright/test'

const jazz = {
  id: 1, title: 'Jazz Night', description: 'Live jazz.', startTime: '2026-11-02T23:00:00Z', endTime: '2026-11-03T02:00:00Z',
  venue: 'Blue Note Club', eventType: 'Music', totalSeats: 120, availableSeats: 48, priceCents: 2500, imageUrl: null,
}

test('the home page lists events from the API', async ({ page }) => {
  await page.route('**/api/v1/events?sort=name', route =>
    route.fulfill({ json: { data: [jazz, { ...jazz, id: 2, title: 'Comedy Showcase' }], errors: [], meta: { nextCursor: null } } }),
  )
  await page.goto('/')

  const list = page.getByRole('list', { name: 'events' })
  await expect(list.getByRole('listitem')).toHaveCount(2)
  await expect(list.getByRole('link', { name: 'Jazz Night', exact: true })).toBeVisible()
  await expect(list.getByText('$25.00').first()).toBeVisible()
})

test('typing in the search box asks the API for matching events', async ({ page }) => {
  await page.route('**/api/v1/events**', route => {
    const q = new URL(route.request().url()).searchParams.get('q')
    const events = q === 'comedy' ? [{ ...jazz, id: 2, title: 'Comedy Showcase' }] : [jazz, { ...jazz, id: 2, title: 'Comedy Showcase' }]
    return route.fulfill({ json: { data: events, errors: [], meta: { nextCursor: null } } })
  })
  await page.goto('/')

  await page.getByRole('textbox', { name: 'Search events' }).fill('comedy')

  const list = page.getByRole('list', { name: 'events' })
  await expect(list.getByRole('listitem')).toHaveCount(1)
  await expect(list.getByRole('link', { name: 'Comedy Showcase', exact: true })).toBeVisible()
})

test('choosing a sort order asks the API to sort', async ({ page }) => {
  await page.route('**/api/v1/events**', route => {
    const sort = new URL(route.request().url()).searchParams.get('sort')
    const events = sort === 'price' ? [{ ...jazz, id: 3, title: 'Cheapest First' }, jazz] : [jazz]
    return route.fulfill({ json: { data: events, errors: [], meta: { nextCursor: null } } })
  })
  await page.goto('/')

  await page.getByRole('combobox', { name: 'Sort events' }).selectOption('price')

  const items = page.getByRole('list', { name: 'events' }).getByRole('listitem')
  await expect(items).toHaveCount(2)
  await expect(items.first()).toContainText('Cheapest First')
})

test('scrolling to the bottom loads the next page of events', async ({ page }) => {
  const pageOne = Array.from({ length: 20 }, (_, i) => ({ ...jazz, id: i + 1, title: `Event ${String(i + 1).padStart(2, '0')}` }))
  await page.route('**/api/v1/events**', route => {
    const after = new URL(route.request().url()).searchParams.get('after')
    return route.fulfill({
      json: after === 'page-2'
        ? { data: [{ ...jazz, id: 21, title: 'Event 21' }], errors: [], meta: { nextCursor: null } }
        : { data: pageOne, errors: [], meta: { nextCursor: 'page-2' } },
    })
  })
  await page.goto('/')
  const items = page.getByRole('list', { name: 'events' }).getByRole('listitem')
  await expect(items).toHaveCount(20)

  await page.getByTestId('events-sentinel').scrollIntoViewIfNeeded()

  await expect(items).toHaveCount(21)
  await expect(page.getByRole('link', { name: 'Event 21', exact: true })).toBeVisible()
})

test('a search with no matches says so', async ({ page }) => {
  await page.route('**/api/v1/events**', route => {
    const q = new URL(route.request().url()).searchParams.get('q')
    return route.fulfill({ json: { data: q ? [] : [jazz], errors: [], meta: { nextCursor: null } } })
  })
  await page.goto('/')

  await page.getByRole('textbox', { name: 'Search events' }).fill('zzz')

  await expect(page.getByText('No events match your search.')).toBeVisible()
})

test('View on an event card opens its detail page', async ({ page }) => {
  await page.route('**/api/v1/events?sort=name', route =>
    route.fulfill({ json: { data: [jazz], errors: [], meta: { nextCursor: null } } }),
  )
  await page.goto('/')

  await page.getByRole('link', { name: 'View Jazz Night' }).click()

  await expect(page).toHaveURL('/events/1')
  await expect(page.getByRole('link', { name: '← Back to events' })).toBeVisible()
})
