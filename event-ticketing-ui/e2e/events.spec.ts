import { expect, test } from '@playwright/test'

const jazz = {
  id: 1, title: 'Jazz Night', description: 'Live jazz.', startTime: '2026-11-02T23:00:00Z', endTime: '2026-11-03T02:00:00Z',
  venue: 'Blue Note Club', eventType: 'Music', totalSeats: 120, availableSeats: 48, priceCents: 2500, imageUrl: null,
}

test('the home page lists events from the API', async ({ page }) => {
  await page.route('**/api/v1/events', route =>
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
