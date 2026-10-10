import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { makeOrder } from '../test/orderFixtures'
import OrderConfirmationPage from './OrderConfirmationPage'

function renderAt(path: string) {
  render(
    <MemoryRouter initialEntries={[path]}>
      <Routes><Route path="/orders/:id" element={<OrderConfirmationPage />} /></Routes>
    </MemoryRouter>,
  )
}

function stubOrder(response: Response) {
  const fetchMock = vi.fn().mockResolvedValue(response)
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

describe('OrderConfirmationPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('confirms the order with event, venue, number and price', async () => {
    stubOrder(new Response(JSON.stringify({ data: makeOrder(), errors: [] })))
    renderAt('/orders/42')

    expect(await screen.findByRole('heading', { name: "You're going!" })).toBeInTheDocument()
    expect(screen.getByText('john@example.com')).toBeInTheDocument()
    expect(screen.getByText('Jazz Night')).toBeInTheDocument()
    expect(screen.getByText(/Blue Note Club/)).toBeInTheDocument()
    expect(screen.getByText(/Order #42/)).toHaveTextContent('Order #42 · $25.00')
    expect(screen.getByRole('link', { name: '← Browse more events' })).toHaveAttribute('href', '/')
  })

  it('sends the guest token from the link', async () => {
    const fetchMock = stubOrder(new Response(JSON.stringify({ data: makeOrder(), errors: [] })))
    renderAt('/orders/42?token=secret-token')

    await screen.findByRole('heading', { name: "You're going!" })
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/orders/42', { credentials: 'include', headers: { 'X-Order-Token': 'secret-token' } })
  })

  it('says the order was not found on a 404', async () => {
    stubOrder(new Response('{}', { status: 404 }))
    renderAt('/orders/42')

    expect(await screen.findByText('Order not found.')).toBeInTheDocument()
  })

  it('shows a generic error for other failures', async () => {
    stubOrder(new Response('{}', { status: 500 }))
    renderAt('/orders/42')

    expect(await screen.findByText('Something went wrong. Please try again.')).toBeInTheDocument()
  })
})
