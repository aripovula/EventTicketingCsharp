import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import type { EventItem } from '../api/events'
import { makeEvent } from '../test/fixtures'
import { colorForType } from '../utils/eventTypes'
import AdminCalendarPage from './AdminCalendarPage'

// FullCalendar needs real layout; in unit tests a stub lists the events it was given.
vi.mock('@fullcalendar/daygrid', () => ({ default: {} }))
type StubEvent = { id: string; title: string; start: string; backgroundColor: string; extendedProps: object }
type StubProps = {
  events: StubEvent[]
  eventMouseEnter?: (arg: object) => void
  eventMouseLeave?: () => void
  eventClick?: (arg: object) => void
}
vi.mock('@fullcalendar/react', () => ({
  default: ({ events, eventMouseEnter, eventMouseLeave, eventClick }: StubProps) => (
    <ul aria-label="calendar events">
      {events.map(e => (
        <li
          key={e.id}
          data-start={e.start}
          data-color={e.backgroundColor}
          onMouseEnter={ev => eventMouseEnter?.({ event: { extendedProps: e.extendedProps }, el: ev.currentTarget })}
          onMouseLeave={() => eventMouseLeave?.()}
          onClick={ev => eventClick?.({ event: { extendedProps: e.extendedProps }, el: ev.currentTarget })}
        >
          {e.title}
        </li>
      ))}
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

  it('explains how the page works', async () => {
    stubEvents([])
    renderPage()

    const note = await screen.findByRole('complementary', { name: 'How this page works' })
    expect(note).toHaveTextContent('FullCalendar')
    expect(note).toHaveTextContent('cursor-paginated')
  })

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

  it('shows remaining seats in a tooltip while hovering an event', async () => {
    stubEvents([makeEvent()])
    renderPage()
    const item = await screen.findByText('Jazz Night – 48')

    await userEvent.hover(item)
    expect(screen.getByRole('tooltip')).toHaveTextContent('Jazz Night48 seats left')

    await userEvent.unhover(item)
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument()
  })

  it('opens a details popup with an edit link when an event is clicked', async () => {
    stubEvents([makeEvent({ id: 7 })])
    renderPage()

    await userEvent.click(await screen.findByText('Jazz Night – 48'))

    const popup = screen.getByRole('dialog', { name: 'Jazz Night' })
    expect(popup).toHaveTextContent('Blue Note Club')
    expect(popup).toHaveTextContent('48 / 120 seats remaining')
    expect(popup).toHaveTextContent('$25.00')
    expect(within(popup).getByRole('link', { name: 'Edit' })).toHaveAttribute('href', '/admin/7/edit')
  })

  it('closes the popup with the close button', async () => {
    stubEvents([makeEvent()])
    renderPage()
    await userEvent.click(await screen.findByText('Jazz Night – 48'))

    await userEvent.click(screen.getByRole('button', { name: 'Close' }))

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('closes the popup when clicking outside it', async () => {
    stubEvents([makeEvent()])
    renderPage()
    await userEvent.click(await screen.findByText('Jazz Night – 48'))

    await userEvent.click(screen.getByRole('heading', { name: 'Events Calendar' }))

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})
