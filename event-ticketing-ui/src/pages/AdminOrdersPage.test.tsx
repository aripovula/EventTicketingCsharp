import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { makeOrder } from '../test/orderFixtures'
import AdminOrdersPage from './AdminOrdersPage'

class FakeIntersectionObserver {
  observe() {}
  disconnect() {}
}

function renderPage() {
  render(<MemoryRouter><AdminOrdersPage /></MemoryRouter>)
}

describe('AdminOrdersPage', () => {
  beforeEach(() => vi.stubGlobal('IntersectionObserver', FakeIntersectionObserver))
  afterEach(() => vi.unstubAllGlobals())

  it('lists every order in a table', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: [
      makeOrder(), makeOrder({ id: 43, eventTitle: 'Comedy Showcase', email: 'jane@example.com', priceCents: 1800 }),
    ], errors: [], meta: { nextCursor: null } })))
    vi.stubGlobal('fetch', fetchMock)
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
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: [], errors: [], meta: { nextCursor: null } }))))
    renderPage()

    expect(await screen.findByText('No orders yet.')).toBeInTheDocument()
  })

  it('says when orders cannot be loaded', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 403 })))
    renderPage()

    expect(await screen.findByText('Failed to load orders.')).toBeInTheDocument()
  })

  it('links back to admin', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: [], errors: [], meta: { nextCursor: null } }))))
    renderPage()

    expect(await screen.findByRole('link', { name: '← Back to admin' })).toHaveAttribute('href', '/admin')
  })

  it('filters the table to one event', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: [
      makeOrder(), makeOrder({ id: 43, eventTitle: 'Comedy Showcase' }), makeOrder({ id: 44 }),
    ], errors: [], meta: { nextCursor: null } }))))
    renderPage()
    const table = await screen.findByRole('table')

    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Filter by event' }), 'Jazz Night')

    const rows = within(table).getAllByRole('row').slice(1)
    expect(rows.map(r => r.textContent)).toEqual([expect.stringContaining('#42'), expect.stringContaining('#44')])
  })

  it('offers each event once in the filter', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: [
      makeOrder(), makeOrder({ id: 43, eventTitle: 'Comedy Showcase' }), makeOrder({ id: 44 }),
    ], errors: [], meta: { nextCursor: null } }))))
    renderPage()

    const select = await screen.findByRole('combobox', { name: 'Filter by event' })
    expect(within(select).getAllByRole('option').map(o => o.textContent)).toEqual(['All events', 'Comedy Showcase', 'Jazz Night'])
  })
})
