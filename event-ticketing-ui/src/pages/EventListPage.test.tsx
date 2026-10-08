import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import type { EventItem } from '../api/events'
import EventListPage from './EventListPage'

export function makeEvent(overrides: Partial<EventItem> = {}): EventItem {
  return {
    id: 1,
    title: 'Jazz Night',
    description: 'Live jazz.',
    startTime: '2026-11-02T23:00:00Z',
    endTime: '2026-11-03T02:00:00Z',
    venue: 'Blue Note Club',
    eventType: 'Music',
    totalSeats: 120,
    availableSeats: 48,
    priceCents: 2500,
    imageUrl: 'https://example.com/jazz.jpg',
    ...overrides,
  }
}

function stubEvents(events: EventItem[]) {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
    new Response(JSON.stringify({ data: events, errors: [], meta: { nextCursor: null } }))))
}

function renderPage() {
  render(<MemoryRouter><EventListPage /></MemoryRouter>)
}

describe('EventListPage', () => {
  afterEach(() => vi.unstubAllGlobals())

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

    const card = within((await screen.findAllByRole('listitem'))[0])
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
})
