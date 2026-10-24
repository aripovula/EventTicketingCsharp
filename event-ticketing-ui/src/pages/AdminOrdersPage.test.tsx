import { act, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { makeEvent } from '../test/fixtures'
import { makeOrder } from '../test/orderFixtures'
import AdminOrdersPage from './AdminOrdersPage'

const hub = vi.hoisted(() => ({ handlers: {} as Record<string, (eventId: number) => void> }))
vi.mock('../hooks/useHubEvents', () => ({
  useHubEvents: (handlers: Record<string, (eventId: number) => void>) => { hub.handlers = handlers },
}))

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

  it('explains how the page works', async () => {
    routeFetch()
    renderPage()

    const note = await screen.findByRole('complementary', { name: 'How this page works' })
    expect(note).toHaveTextContent('cursor pagination')
    expect(note).toHaveTextContent('SignalR')
  })

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

  it('shows a new order as soon as it is booked', async () => {
    const fetchMock = routeFetch({ orders: json([makeOrder()]) })
    renderPage()
    await screen.findByText('#42')
    fetchMock.mockImplementation((url: string) => Promise.resolve(
      url.startsWith('/api/v1/events') ? json([]) : json([makeOrder({ id: 50 }), makeOrder()])))

    act(() => hub.handlers.BookingMade(1))

    expect(await screen.findByText('#50')).toBeInTheDocument()
  })

  it('ignores bookings for other events while filtered', async () => {
    const fetchMock = routeFetch({ events: [makeEvent({ id: 7, title: 'Comedy Showcase' })] })
    renderPage()
    await screen.findByRole('table')
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Filter by event' }), 'Comedy Showcase')
    await screen.findByText('#43')
    fetchMock.mockClear()

    act(() => hub.handlers.BookingMade(1))

    expect(fetchMock).not.toHaveBeenCalled()
  })

  it.each(['EventCreated', 'EventUpdated', 'EventArchived'])('reloads the filter\'s events when %s arrives', async message => {
    const events = [makeEvent({ id: 1, title: 'Jazz Night' })]
    routeFetch({ events })
    renderPage()
    const select = await screen.findByRole('combobox', { name: 'Filter by event' })
    events.push(makeEvent({ id: 9, title: 'Art Fair' }))

    act(() => hub.handlers[message](9))

    await waitFor(() =>
      expect(within(select).getAllByRole('option').map(o => o.textContent)).toEqual(['All events', 'Art Fair', 'Jazz Night']))
  })

  it('goes back to all events when the chosen event is deleted', async () => {
    const events = [makeEvent({ id: 1, title: 'Jazz Night' }), makeEvent({ id: 9, title: 'Art Fair' })]
    const fetchMock = routeFetch({ events })
    renderPage()
    await screen.findByRole('table')
    const select = screen.getByRole('combobox', { name: 'Filter by event' })
    await userEvent.selectOptions(select, 'Art Fair')
    await screen.findByText('No orders for this event.')
    events.pop()
    fetchMock.mockClear()

    act(() => hub.handlers.EventDeleted(9))

    await waitFor(() => expect(within(select).getAllByRole('option')).toHaveLength(2))
    expect(select).toHaveValue('')
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/admin/orders', { credentials: 'include' })
  })
})
