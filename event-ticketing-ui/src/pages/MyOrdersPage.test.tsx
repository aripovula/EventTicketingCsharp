import { act, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { AuthContext, type User } from '../context/authContext'
import { makeOrder } from '../test/orderFixtures'
import MyOrdersPage from './MyOrdersPage'

function renderWithUser(ui: React.ReactNode, user: User | null, loading = false) {
  render(
    <AuthContext.Provider value={{ user, loading, signIn: () => {}, signOut: async () => {} }}>
      <MemoryRouter>{ui}</MemoryRouter>
    </AuthContext.Provider>,
  )
}

const john: User = { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }

let intersect: () => void = () => {}
class FakeIntersectionObserver {
  constructor(callback: IntersectionObserverCallback) {
    intersect = () => callback([{ isIntersecting: true } as IntersectionObserverEntry], this as unknown as IntersectionObserver)
  }
  observe() {}
  disconnect() {}
}

describe('MyOrdersPage', () => {
  beforeEach(() => vi.stubGlobal('IntersectionObserver', FakeIntersectionObserver))
  afterEach(() => vi.unstubAllGlobals())

  it('renders the my-orders heading', () => {
    renderWithUser(<MyOrdersPage />, null)

    expect(screen.getByRole('heading', { name: 'My orders' })).toBeInTheDocument()
  })

  it('asks signed-out visitors to sign in', () => {
    renderWithUser(<MyOrdersPage />, null)

    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/login')
  })

  it('waits for the session check', () => {
    renderWithUser(<MyOrdersPage />, null, true)

    expect(screen.queryByRole('heading', { name: 'My orders' })).not.toBeInTheDocument()
  })

  it('lists the signed-in user orders with a confirmation link', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(
      { data: [makeOrder(), makeOrder({ id: 43, eventTitle: 'Comedy Showcase' })], errors: [], meta: { nextCursor: null } }))))
    renderWithUser(<MyOrdersPage />, john)

    const list = await screen.findByRole('list', { name: 'orders' })
    expect(within(list).getAllByRole('listitem')).toHaveLength(2)
    expect(within(list).getAllByText(/Order #42/)[0]).toHaveTextContent('Order #42 · $25.00')
    expect(within(list).getAllByRole('link', { name: 'View confirmation →' })[1]).toHaveAttribute('href', '/orders/43')
  })

  it('says when there are no orders', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: [], errors: [], meta: { nextCursor: null } }))))
    renderWithUser(<MyOrdersPage />, john)

    expect(await screen.findByText('No orders found.')).toBeInTheDocument()
  })

  it('shows an error when the orders cannot be loaded', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 500 })))
    renderWithUser(<MyOrdersPage />, john)

    expect(await screen.findByText('Something went wrong. Please try again.')).toBeInTheDocument()
  })

  it('loads more orders when scrolled to the end', async () => {
    const fetchMock = vi.fn<(url: string) => Promise<Response>>(url => Promise.resolve(new Response(JSON.stringify(
      url.includes('after=c1')
        ? { data: [makeOrder({ id: 2, eventTitle: 'Second' })], errors: [], meta: { nextCursor: null } }
        : { data: [makeOrder({ id: 1, eventTitle: 'First' })], errors: [], meta: { nextCursor: 'c1' } }))))
    vi.stubGlobal('fetch', fetchMock)
    renderWithUser(<MyOrdersPage />, john)
    await screen.findByText('First')

    await waitFor(() => {
      act(() => intersect())
      expect(fetchMock.mock.calls).toHaveLength(2)
    })

    expect(await screen.findByText('Second')).toBeInTheDocument()
  })

  it('does not request orders for a signed-out visitor', () => {
    const fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)
    renderWithUser(<MyOrdersPage />, null)

    expect(fetchMock).not.toHaveBeenCalled()
  })
})
