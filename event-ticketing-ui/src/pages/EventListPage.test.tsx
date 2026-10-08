import { render, screen, within } from '@testing-library/react'
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
})
