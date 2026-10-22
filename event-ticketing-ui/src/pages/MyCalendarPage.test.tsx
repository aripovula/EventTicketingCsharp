import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthContext, type User } from '../context/authContext'
import { makeOrder } from '../test/orderFixtures'
import { colorForType } from '../utils/eventTypes'
import MyCalendarPage from './MyCalendarPage'

vi.mock('@fullcalendar/daygrid', () => ({ default: {} }))
type StubEvent = { id: string; title: string; start: string; backgroundColor: string; extendedProps: object }
type StubProps = { events: StubEvent[]; eventMouseEnter?: (arg: object) => void; eventMouseLeave?: () => void; eventClick?: (arg: object) => void }
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

const john: User = { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }

function renderAs(user: User | null) {
  render(
    <AuthContext.Provider value={{ user, loading: false, signIn: () => {}, signOut: async () => {} }}>
      <MemoryRouter initialEntries={['/calendar']}>
        <Routes>
          <Route path="/login" element={<p>Login page</p>} />
          <Route path="/calendar" element={<MyCalendarPage />} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

describe('MyCalendarPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('explains how the page works', () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: [], errors: [], meta: { nextCursor: null } }))))
    renderAs(john)

    const note = screen.getByRole('complementary', { name: 'How this page works' })
    expect(note).toHaveTextContent('FullCalendar')
    expect(note).toHaveTextContent('UTC')
  })

  it('renders the My Calendar heading for a signed-in user', () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: [], errors: [], meta: { nextCursor: null } }))))
    renderAs(john)

    expect(screen.getByRole('heading', { name: 'My Calendar' })).toBeInTheDocument()
  })

  it('sends a signed-out visitor to the login page', () => {
    renderAs(null)

    expect(screen.getByText('Login page')).toBeInTheDocument()
  })

  it('puts every booked event on the calendar, coloured by type', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: [
      makeOrder(), makeOrder({ id: 43, eventTitle: 'Comedy Showcase', eventType: 'Comedy' }),
    ], errors: [], meta: { nextCursor: null } }))))
    renderAs(john)

    await screen.findByText('Comedy Showcase')
    const items = within(screen.getByRole('list', { name: 'calendar events' })).getAllByRole('listitem')
    expect(items.map(i => i.textContent)).toEqual(['Jazz Night', 'Comedy Showcase'])
    expect(screen.getByText('Comedy Showcase')).toHaveAttribute('data-color', colorForType('Comedy'))
  })

  it('says when bookings cannot be loaded', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 500 })))
    renderAs(john)

    expect(await screen.findByText('Could not load your bookings.')).toBeInTheDocument()
  })

  function stubOrders() {
    const start = new Date(2026, 10, 2, 19, 0)
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: [
      makeOrder({ eventStartTime: start.toISOString(), eventEndTime: new Date(2026, 10, 2, 22, 0).toISOString() }),
    ], errors: [], meta: { nextCursor: null } }))))
  }

  it('shows the event time in a tooltip while hovering a booking', async () => {
    stubOrders()
    renderAs(john)
    const item = await screen.findByText('Jazz Night')

    await userEvent.hover(item)
    expect(screen.getByRole('tooltip')).toHaveTextContent('Jazz Night19:00 – 22:00')

    await userEvent.unhover(item)
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument()
  })

  it('opens the booking details with a link to the order when clicked', async () => {
    stubOrders()
    renderAs(john)

    await userEvent.click(await screen.findByText('Jazz Night'))

    const popup = screen.getByRole('dialog', { name: 'Jazz Night' })
    expect(popup).toHaveTextContent('Music')
    expect(popup).toHaveTextContent('Blue Note Club')
    expect(popup).toHaveTextContent('19:00 – 22:00')
    expect(popup).toHaveTextContent('Order #42')
    expect(popup).toHaveTextContent('$25.00')
    expect(within(popup).getByRole('link', { name: 'View order' })).toHaveAttribute('href', '/orders/42')
  })

  it('closes the booking details with the close button', async () => {
    stubOrders()
    renderAs(john)
    await userEvent.click(await screen.findByText('Jazz Night'))

    await userEvent.click(screen.getByRole('button', { name: 'Close' }))

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('closes the booking details when clicking outside them', async () => {
    stubOrders()
    renderAs(john)
    await userEvent.click(await screen.findByText('Jazz Night'))

    await userEvent.click(screen.getByRole('heading', { name: 'My Calendar' }))

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})
