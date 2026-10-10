import { render, screen, within } from '@testing-library/react'
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
})
