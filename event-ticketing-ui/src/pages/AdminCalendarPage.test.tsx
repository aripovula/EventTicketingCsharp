import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import type { EventItem } from '../api/events'
import { makeEvent } from '../test/fixtures'
import { colorForType } from '../utils/eventTypes'
import AdminCalendarPage from './AdminCalendarPage'

// FullCalendar needs real layout; in unit tests a stub lists the events it was given.
vi.mock('@fullcalendar/daygrid', () => ({ default: {} }))
vi.mock('@fullcalendar/react', () => ({
  default: ({ events }: { events: { id: string; title: string; start: string; backgroundColor: string }[] }) => (
    <ul aria-label="calendar events">
      {events.map(e => <li key={e.id} data-start={e.start} data-color={e.backgroundColor}>{e.title}</li>)}
    </ul>
  ),
}))

function stubEvents(events: EventItem[]) {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
    new Response(JSON.stringify({ data: events, errors: [], meta: { nextCursor: null } }))))
}

function renderPage() {
  render(<MemoryRouter><AdminCalendarPage /></MemoryRouter>)
}

describe('AdminCalendarPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('renders the heading and a link back to admin', async () => {
    stubEvents([])
    renderPage()

    expect(await screen.findByRole('heading', { name: 'Events Calendar' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '← Back to admin' })).toHaveAttribute('href', '/admin')
  })

  it('shows a colour legend for every event type except Other', async () => {
    stubEvents([])
    renderPage()

    const legend = within(await screen.findByRole('list', { name: 'event type legend' }))
    expect(legend.getAllByRole('listitem')).toHaveLength(8)
    expect(legend.queryByText('Other')).not.toBeInTheDocument()
  })

  it('puts every event on the calendar as title and remaining seats, coloured by type', async () => {
    stubEvents([makeEvent(), makeEvent({ id: 2, title: 'Comedy Showcase', eventType: 'Comedy', availableSeats: 134 })])
    renderPage()

    const items = within(await screen.findByRole('list', { name: 'calendar events' })).getAllByRole('listitem')
    expect(items.map(i => i.textContent)).toEqual(['Jazz Night – 48', 'Comedy Showcase – 134'])
    expect(items[1]).toHaveAttribute('data-color', colorForType('Comedy'))
    expect(items[0]).toHaveAttribute('data-start', makeEvent().startTime)
  })

  it('says when events cannot be loaded', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 500 })))
    renderPage()

    expect(await screen.findByText('Failed to load events (500).')).toBeInTheDocument()
  })
})
