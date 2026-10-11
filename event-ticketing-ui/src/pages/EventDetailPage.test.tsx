import { act, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useParams, useSearchParams } from 'react-router-dom'
import type { EventItem } from '../api/events'
import { AuthContext, type User } from '../context/authContext'
import EventDetailPage from './EventDetailPage'
import { makeEvent } from '../test/fixtures'

const hub = vi.hoisted(() => ({ handlers: {} as Record<string, (eventId: number) => void> }))
vi.mock('../hooks/useHubEvents', () => ({
  useHubEvents: (handlers: Record<string, (eventId: number) => void>) => { hub.handlers = handlers },
}))

function stubEvent(event: EventItem) {
  const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: event, errors: [] })))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function OrderProbe() {
  const { id } = useParams()
  const [searchParams] = useSearchParams()
  return <p>Order page {id}{searchParams.get('token') ? ` with token ${searchParams.get('token')}` : ''}</p>
}

function renderPage(id = 1, user: User | null = null) {
  render(
    <AuthContext.Provider value={{ user, loading: false, signIn: () => {}, signOut: async () => {} }}>
      <MemoryRouter initialEntries={[`/events/${id}`]}>
        <Routes>
          <Route path="/events/:id" element={<EventDetailPage />} />
          <Route path="/orders/:id" element={<OrderProbe />} />
          <Route path="/" element={<p>Home page</p>} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

describe('EventDetailPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('links back to the events list', async () => {
    stubEvent(makeEvent())
    renderPage()

    expect(await screen.findByRole('link', { name: '← Back to events' })).toHaveAttribute('href', '/')
  })

  it('loads the event named in the URL', async () => {
    const fetchMock = stubEvent(makeEvent({ id: 7 }))
    renderPage(7)

    await screen.findByRole('heading', { name: 'Jazz Night' })
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events/7')
  })

  it('shows the event details, price and seats', async () => {
    stubEvent(makeEvent())
    renderPage()

    expect(await screen.findByRole('heading', { name: 'Jazz Night' })).toBeInTheDocument()
    expect(screen.getByText('Live jazz.')).toBeInTheDocument()
    expect(screen.getByText(/Blue Note Club/)).toBeInTheDocument()
    expect(screen.getByText('$25.00')).toBeInTheDocument()
    expect(screen.getByText('48 of 120 seats available')).toBeInTheDocument()
  })

  it('shows sold out when no seats are left', async () => {
    stubEvent(makeEvent({ availableSeats: 0 }))
    renderPage()

    // Both the seat line and the disabled button say so.
    expect(await screen.findAllByText('Sold out')).toHaveLength(2)
  })

  it('names the browser tab after the event', async () => {
    stubEvent(makeEvent())
    renderPage()

    await screen.findByRole('heading', { name: 'Jazz Night' })
    // The title is set in an effect, which can run just after the heading renders.
    await waitFor(() => expect(document.title).toBe('Jazz Night | Ticketing'))
  })

  it('says the event was not found on a 404', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 404 })))
    renderPage(999)

    expect(await screen.findByText('Event not found.')).toBeInTheDocument()
  })

  it('shows a generic error for other failures', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 500 })))
    renderPage()

    expect(await screen.findByText('Something went wrong. Please try again.')).toBeInTheDocument()
  })

  it('shows a generic error when the request fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))
    renderPage()

    expect(await screen.findByText('Something went wrong. Please try again.')).toBeInTheDocument()
  })

  function stubEventThenBooking(bookingResponse: Response, seatsAfter = 47) {
    let booked = false
    const fetchMock = vi.fn((_: string, init?: RequestInit) => {
      if (init?.method === 'POST') { booked = true; return Promise.resolve(bookingResponse) }
      return Promise.resolve(new Response(JSON.stringify({ data: makeEvent({ availableSeats: booked ? seatsAfter : 48 }), errors: [] })))
    })
    vi.stubGlobal('fetch', fetchMock)
    return fetchMock
  }

  it('sends one idempotency key per booking attempt', async () => {
    const fetchMock = stubEventThenBooking(new Response('oops', { status: 500 }))
    fetchMock.mockImplementation((_: string, init?: RequestInit) => Promise.resolve(init?.method === 'POST'
      ? new Response('oops', { status: 500 })
      : new Response(JSON.stringify({ data: makeEvent(), errors: [] }))))
    renderPage()
    const keys = () => fetchMock.mock.calls
      .filter(([, init]) => init?.method === 'POST')
      .map(([, init]) => (init!.headers as Record<string, string>)['Idempotency-Key'])

    await userEvent.click(await screen.findByRole('button', { name: 'Buy ticket' }))
    await userEvent.type(screen.getByLabelText('Email'), 'guest@example.com')
    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))
    await userEvent.click(await screen.findByRole('button', { name: 'Place order' }))
    await userEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    await userEvent.click(screen.getByRole('button', { name: 'Buy ticket' }))
    await userEvent.type(screen.getByLabelText('Email'), 'guest@example.com')
    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))

    await waitFor(() => expect(keys()).toHaveLength(3))
    const [first, retry, reopened] = keys()
    expect(first).toMatch(/^[0-9a-f-]{36}$/)
    expect(retry).toBe(first)
    expect(reopened).not.toBe(first)
  })

  it('disables buying when sold out', async () => {
    stubEvent(makeEvent({ availableSeats: 0 }))
    renderPage()

    expect(await screen.findByRole('button', { name: 'Sold out' })).toBeDisabled()
  })

  it('opens the booking modal with the signed-in user email', async () => {
    stubEvent(makeEvent())
    renderPage(1, { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' })

    await userEvent.click(await screen.findByRole('button', { name: 'Buy ticket' }))

    expect(within(screen.getByRole('dialog', { name: 'Place order' })).getByLabelText('Email')).toHaveValue('john@example.com')
  })

  it('takes a guest to the order page with the lookup token', async () => {
    const order = { id: 42, eventId: 1, eventTitle: 'Jazz Night', email: 'guest@example.com', priceCents: 2500, bookedAt: '2026-10-09T12:00:00Z', guestAccessToken: 'secret' }
    const fetchMock = stubEventThenBooking(new Response(JSON.stringify({ data: order, errors: [] }), { status: 201 }))
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: 'Buy ticket' }))
    await userEvent.type(screen.getByLabelText('Email'), 'guest@example.com')
    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))

    expect(await screen.findByText('Order page 42 with token secret')).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events/1/bookings', expect.objectContaining({ method: 'POST' }))
  })

  it('takes a signed-in user to the order page without a token', async () => {
    const order = { id: 43, eventId: 1, eventTitle: 'Jazz Night', email: 'john@example.com', priceCents: 2500, bookedAt: '2026-10-09T12:00:00Z', guestAccessToken: null }
    stubEventThenBooking(new Response(JSON.stringify({ data: order, errors: [] }), { status: 201 }))
    renderPage(1, { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' })

    await userEvent.click(await screen.findByRole('button', { name: 'Buy ticket' }))
    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))

    expect(await screen.findByText('Order page 43')).toBeInTheDocument()
  })

  it('says when the last seat was just taken', async () => {
    stubEventThenBooking(new Response(JSON.stringify({ data: null, errors: [{ code: 'sold_out', detail: 'Sorry, this event just sold out.' }] }), { status: 409 }))
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: 'Buy ticket' }))
    await userEvent.type(screen.getByLabelText('Email'), 'guest@example.com')
    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Sorry, this event just sold out.')
  })

  it('asks the visitor to wait when booking too often', async () => {
    stubEventThenBooking(new Response(JSON.stringify({ data: null, errors: [{ code: 'rate_limited' }] }), { status: 429, headers: { 'Retry-After': '40' } }))
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: 'Buy ticket' }))
    await userEvent.type(screen.getByLabelText('Email'), 'guest@example.com')
    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Too many booking attempts. Please try again in 40 seconds.')
  })

  it('asks for a minute when the wait is unknown', async () => {
    stubEventThenBooking(new Response('{}', { status: 429 }))
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: 'Buy ticket' }))
    await userEvent.type(screen.getByLabelText('Email'), 'guest@example.com')
    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Too many booking attempts. Please wait a minute and try again.')
  })

  it('reports other booking failures', async () => {
    stubEventThenBooking(new Response('oops', { status: 500 }))
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: 'Buy ticket' }))
    await userEvent.type(screen.getByLabelText('Email'), 'guest@example.com')
    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Booking failed. Please try again.')
  })

  describe('live updates', () => {
    function stubSeatsSequence(...seats: number[]) {
      const fetchMock = vi.fn()
      for (const availableSeats of seats) {
        fetchMock.mockImplementationOnce(() => Promise.resolve(new Response(JSON.stringify({ data: makeEvent({ availableSeats }), errors: [] }))))
      }
      vi.stubGlobal('fetch', fetchMock)
      return fetchMock
    }

    it('refreshes the seats when this event is booked', async () => {
      stubSeatsSequence(48, 47)
      renderPage(1)
      await screen.findByText('48 of 120 seats available')

      act(() => hub.handlers.BookingMade(1))

      expect(await screen.findByText('47 of 120 seats available')).toBeInTheDocument()
    })

    it('refreshes the details when this event is edited', async () => {
      const fetchMock = stubSeatsSequence(48, 48)
      renderPage(1)
      await screen.findByText('48 of 120 seats available')

      act(() => hub.handlers.EventUpdated(1))

      await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2))
    })

    it('ignores updates about other events', async () => {
      const fetchMock = stubSeatsSequence(48)
      renderPage(1)
      await screen.findByText('48 of 120 seats available')

      act(() => {
        hub.handlers.BookingMade(2)
        hub.handlers.EventUpdated(2)
        hub.handlers.EventDeleted(2)
      })

      expect(fetchMock).toHaveBeenCalledTimes(1)
      expect(screen.getByText('48 of 120 seats available')).toBeInTheDocument()
    })

    it('goes to the home page when this event is deleted', async () => {
      stubSeatsSequence(48)
      renderPage(1)
      await screen.findByText('48 of 120 seats available')

      act(() => hub.handlers.EventDeleted(1))

      expect(await screen.findByText('Home page')).toBeInTheDocument()
    })
  })
})
