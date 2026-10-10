import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { makeEvent } from '../test/fixtures'
import { makeOrder } from '../test/orderFixtures'
import AdminOrdersPage from './AdminOrdersPage'

class FakeIntersectionObserver {
  observe() {}
  disconnect() {}
}

const json = (data: unknown) => new Response(JSON.stringify({ data, errors: [], meta: { nextCursor: null } }))

const comedyOrder = makeOrder({ id: 43, eventId: 7, eventTitle: 'Comedy Showcase', email: 'jane@example.com', priceCents: 1800 })

/** Answers the events list and the admin orders list (eventId 7 → only order #43, any other filter → none). */
function routeFetch({ events = [], orders = json([makeOrder(), comedyOrder]) }: { events?: ReturnType<typeof makeEvent>[]; orders?: Response } = {}) {
  const fetchMock = vi.fn((url: string) => {
    if (url.startsWith('/api/v1/events')) return Promise.resolve(json(events))
    if (url.includes('eventId=7')) return Promise.resolve(json([comedyOrder]))
    if (url.includes('eventId=')) return Promise.resolve(json([]))
    return Promise.resolve(orders)
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function renderPage() {
  render(<MemoryRouter><AdminOrdersPage /></MemoryRouter>)
}

describe('AdminOrdersPage', () => {
  beforeEach(() => vi.stubGlobal('IntersectionObserver', FakeIntersectionObserver))
  afterEach(() => vi.unstubAllGlobals())

  it('lists every order in a table', async () => {
    const fetchMock = routeFetch()
    renderPage()

    const rows = within(await screen.findByRole('table')).getAllByRole('row')
    expect(rows).toHaveLength(3)
    expect(rows[2]).toHaveTextContent('#43')
    expect(rows[2]).toHaveTextContent('Comedy Showcase')
    expect(rows[2]).toHaveTextContent('jane@example.com')
    expect(rows[2]).toHaveTextContent('$18.00')
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/admin/orders', { credentials: 'include' })
  })

  it('says when there are no orders yet', async () => {
    routeFetch({ orders: json([]) })
    renderPage()

    expect(await screen.findByText('No orders yet.')).toBeInTheDocument()
  })

  it('says when orders cannot be loaded', async () => {
    routeFetch({ orders: new Response('{}', { status: 403 }) })
    renderPage()

    expect(await screen.findByText('Failed to load orders.')).toBeInTheDocument()
  })

  it('links back to admin', async () => {
    routeFetch({ orders: json([]) })
    renderPage()

    expect(await screen.findByRole('link', { name: '← Back to admin' })).toHaveAttribute('href', '/admin')
  })

  it('asks the server for one event\'s orders when filtered', async () => {
    const fetchMock = routeFetch({ events: [makeEvent({ id: 7, title: 'Comedy Showcase' })] })
    renderPage()
    await screen.findByRole('table')

    fetchMock.mockClear()
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Filter by event' }), 'Comedy Showcase')

    expect(await screen.findByText('#43')).toBeInTheDocument()
    expect(screen.queryByText('#42')).not.toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/admin/orders?eventId=7', { credentials: 'include' })
  })

  it('offers every event in the filter, alphabetically', async () => {
    routeFetch({ events: [makeEvent({ id: 1, title: 'Jazz Night' }), makeEvent({ id: 9, title: 'Art Fair' })] })
    renderPage()

    const select = await screen.findByRole('combobox', { name: 'Filter by event' })
    await waitFor(() =>
      expect(within(select).getAllByRole('option').map(o => o.textContent)).toEqual(['All events', 'Art Fair', 'Jazz Night']))
  })

  it('says when the chosen event has no orders', async () => {
    routeFetch({ events: [makeEvent({ id: 9, title: 'Art Fair' })] })
    renderPage()
    await screen.findByRole('table')

    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Filter by event' }), 'Art Fair')

    expect(await screen.findByText('No orders for this event.')).toBeInTheDocument()
  })
})
