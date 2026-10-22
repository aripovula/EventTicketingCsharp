import { act, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import type { EventItem } from '../api/events'
import { makeEvent } from '../test/fixtures'

const hub = vi.hoisted(() => ({ handlers: {} as Record<string, (eventId: number) => void> }))
vi.mock('../hooks/useHubEvents', () => ({
  useHubEvents: (handlers: Record<string, (eventId: number) => void>) => { hub.handlers = handlers },
}))
import EventListPage from './EventListPage'

function stubEvents(events: EventItem[]) {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
    new Response(JSON.stringify({ data: events, errors: [], meta: { nextCursor: null } }))))
}

function renderPage() {
  render(<MemoryRouter><EventListPage /></MemoryRouter>)
}

// happy-dom does no layout, so tests trigger the infinite-scroll sentinel by hand.
let intersect: () => void = () => {}
class FakeIntersectionObserver {
  constructor(callback: IntersectionObserverCallback) {
    intersect = () => callback([{ isIntersecting: true } as IntersectionObserverEntry], this as unknown as IntersectionObserver)
  }
  observe() {}
  disconnect() {}
}

describe('EventListPage', () => {
  beforeEach(() => vi.stubGlobal('IntersectionObserver', FakeIntersectionObserver))
  afterEach(() => vi.unstubAllGlobals())

  it('explains how the page works', async () => {
    stubEvents([makeEvent()])
    renderPage()

    const note = await screen.findByRole('complementary', { name: 'How this page works' })
    expect(note).toHaveTextContent('SignalR')
    expect(note).toHaveTextContent('cursor pagination')
    expect(note).toHaveTextContent('Redis')
  })

  it('shows a loading message until events arrive', async () => {
    stubEvents([makeEvent()])
    renderPage()

    expect(screen.getByText('Loading events...')).toBeInTheDocument()
    expect(await screen.findByRole('list', { name: 'events' })).toBeInTheDocument()
  })

  it('renders one card per event from the API', async () => {
    stubEvents([makeEvent(), makeEvent({ id: 2, title: 'Comedy Showcase', eventType: 'Comedy' })])
    renderPage()

    const list = await screen.findByRole('list', { name: 'events' })
    expect(within(list).getAllByRole('listitem')).toHaveLength(2)
    expect(screen.getByRole('link', { name: 'Comedy Showcase' })).toHaveAttribute('href', '/events/2')
  })

  it('shows venue, type, seats and price on the card', async () => {
    stubEvents([makeEvent()])
    renderPage()

    const card = within(within(await screen.findByRole('list', { name: 'events' })).getAllByRole('listitem')[0])
    expect(card.getByText(/Blue Note Club/)).toBeInTheDocument()
    expect(card.getByText('Music')).toBeInTheDocument()
    expect(card.getByText('48 seats available')).toBeInTheDocument()
    expect(card.getByText('$25.00')).toBeInTheDocument()
    expect(card.getByRole('link', { name: 'View Jazz Night' })).toHaveAttribute('href', '/events/1')
  })

  it('searches on the server after typing pauses', async () => {
    const fetchMock = vi.fn<(url: string) => Promise<Response>>(() => Promise.resolve(
      new Response(JSON.stringify({ data: [makeEvent()], errors: [], meta: { nextCursor: null } }))))
    vi.stubGlobal('fetch', fetchMock)
    renderPage()
    await screen.findByRole('list', { name: 'events' })

    await userEvent.type(screen.getByRole('textbox', { name: 'Search events' }), 'jazz')

    await waitFor(() => expect(fetchMock).toHaveBeenLastCalledWith('/api/v1/events?q=jazz&sort=name'))
    expect(fetchMock.mock.calls.filter(([url]) => String(url).includes('q='))).toHaveLength(1)
  })

  it('widens the search box on focus and shrinks it on blur', async () => {
    stubEvents([makeEvent()])
    renderPage()
    const input = await screen.findByRole('textbox', { name: 'Search events' })

    await userEvent.click(input)
    expect(input).toHaveStyle({ width: '400px', fontSize: '22px' })

    await userEvent.tab()
    expect(input).toHaveStyle({ width: '200px', fontSize: '12px' })
  })

  it('asks the API for the chosen sort order', async () => {
    const fetchMock = vi.fn<(url: string) => Promise<Response>>(() => Promise.resolve(
      new Response(JSON.stringify({ data: [makeEvent()], errors: [], meta: { nextCursor: null } }))))
    vi.stubGlobal('fetch', fetchMock)
    renderPage()
    await screen.findByRole('list', { name: 'events' })

    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Sort events' }), 'price')

    await waitFor(() => expect(fetchMock).toHaveBeenLastCalledWith('/api/v1/events?sort=price'))
  })

  it('sorts by name by default', async () => {
    const fetchMock = vi.fn<(url: string) => Promise<Response>>(() => Promise.resolve(
      new Response(JSON.stringify({ data: [makeEvent()], errors: [], meta: { nextCursor: null } }))))
    vi.stubGlobal('fetch', fetchMock)
    renderPage()

    expect(await screen.findByRole('combobox', { name: 'Sort events' })).toHaveValue('name')
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events?sort=name')
  })

  // The observer for the loaded page is created by an effect that may run a moment after the
  // cards render, so keep firing the sentinel until that observer asks for the next page.
  async function scrollToNextPage(fetchMock: { mock: { calls: unknown[] } }, expectedCalls: number) {
    await waitFor(() => {
      act(() => intersect())
      expect(fetchMock.mock.calls).toHaveLength(expectedCalls)
    })
  }

  function stubPages(pages: Record<string, { events: EventItem[]; nextCursor: string | null }>) {
    const fetchMock = vi.fn<(url: string) => Promise<Response>>(url => {
      const after = new URL(url, 'http://localhost').searchParams.get('after') ?? 'first'
      const page = pages[after]
      return Promise.resolve(new Response(JSON.stringify({ data: page.events, errors: [], meta: { nextCursor: page.nextCursor } })))
    })
    vi.stubGlobal('fetch', fetchMock)
    return fetchMock
  }

  it('loads and appends the next page when the sentinel scrolls into view', async () => {
    const fetchMock = stubPages({
      first: { events: [makeEvent({ id: 1, title: 'First' })], nextCursor: 'c1' },
      c1: { events: [makeEvent({ id: 2, title: 'Second' })], nextCursor: null },
    })
    renderPage()
    await screen.findByRole('link', { name: 'First' })

    await scrollToNextPage(fetchMock, 2)

    expect(await screen.findByRole('link', { name: 'Second' })).toBeInTheDocument()
    expect(within(screen.getByRole('list', { name: 'events' })).getAllByRole('listitem')).toHaveLength(2)
    expect(fetchMock).toHaveBeenLastCalledWith('/api/v1/events?sort=name&after=c1')
  })

  it('stops loading when there is no next cursor', async () => {
    const fetchMock = stubPages({ first: { events: [makeEvent()], nextCursor: null } })
    renderPage()
    await screen.findByRole('link', { name: 'Jazz Night' })

    act(() => intersect())

    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('starts again from the first page when the sort changes', async () => {
    const fetchMock = stubPages({
      first: { events: [makeEvent({ id: 1, title: 'First' })], nextCursor: 'c1' },
      c1: { events: [makeEvent({ id: 2, title: 'Second' })], nextCursor: null },
    })
    renderPage()
    await screen.findByRole('link', { name: 'First' })
    await scrollToNextPage(fetchMock, 2)
    await screen.findByRole('link', { name: 'Second' })

    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Sort events' }), 'date')

    await waitFor(() => expect(within(screen.getByRole('list', { name: 'events' })).getAllByRole('listitem')).toHaveLength(1))
  })

  it('says so when no events match', async () => {
    stubEvents([])
    renderPage()

    expect(await screen.findByText('No events match your search.')).toBeInTheDocument()
  })

  it('shows no empty message when events exist', async () => {
    stubEvents([makeEvent()])
    renderPage()
    await screen.findByRole('link', { name: 'Jazz Night' })

    expect(screen.queryByText('No events match your search.')).not.toBeInTheDocument()
  })

  describe('live updates', () => {
    function stubListThenEvent(list: EventItem[], refreshed?: EventItem) {
      const fetchMock = vi.fn((url: string) => Promise.resolve(new Response(JSON.stringify(
        url.startsWith('/api/v1/events?')
          ? { data: list, errors: [], meta: { nextCursor: null } }
          : { data: refreshed, errors: [] }))))
      vi.stubGlobal('fetch', fetchMock)
      return fetchMock
    }

    it('refreshes the seats on a card when its event is booked', async () => {
      stubListThenEvent([makeEvent({ availableSeats: 48 })], makeEvent({ availableSeats: 47 }))
      renderPage()
      await screen.findByText('48 seats available')

      act(() => hub.handlers.BookingMade(1))

      expect(await screen.findByText('47 seats available')).toBeInTheDocument()
    })

    it('refreshes a card when its event is edited', async () => {
      stubListThenEvent([makeEvent()], makeEvent({ title: 'Jazz Night Deluxe' }))
      renderPage()
      await screen.findByText('Jazz Night')

      act(() => hub.handlers.EventUpdated(1))

      expect(await screen.findByText('Jazz Night Deluxe')).toBeInTheDocument()
    })

    it('ignores updates for events that are not on screen', async () => {
      const fetchMock = stubListThenEvent([makeEvent()])
      renderPage()
      await screen.findByText('Jazz Night')

      act(() => hub.handlers.BookingMade(99))

      expect(fetchMock).toHaveBeenCalledTimes(1)
    })

    it('reloads the list when an event is created', async () => {
      const fetchMock = stubListThenEvent([makeEvent()])
      renderPage()
      await screen.findByText('Jazz Night')
      fetchMock.mockImplementation(() => Promise.resolve(new Response(JSON.stringify(
        { data: [makeEvent(), makeEvent({ id: 3, title: 'Brand New Gig' })], errors: [], meta: { nextCursor: null } }))))

      act(() => hub.handlers.EventCreated(3))

      expect(await screen.findByText('Brand New Gig')).toBeInTheDocument()
    })

    it('removes a card when its event is deleted', async () => {
      stubListThenEvent([makeEvent(), makeEvent({ id: 2, title: 'Comedy Showcase' })])
      renderPage()
      await screen.findByText('Comedy Showcase')

      act(() => hub.handlers.EventDeleted(2))

      expect(screen.queryByText('Comedy Showcase')).not.toBeInTheDocument()
      expect(screen.getByText('Jazz Night')).toBeInTheDocument()
    })
  })
})
