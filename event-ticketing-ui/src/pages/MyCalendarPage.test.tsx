import { render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthContext, type User } from '../context/authContext'
import { makeOrder } from '../test/orderFixtures'
import { colorForType } from '../utils/eventTypes'
import MyCalendarPage from './MyCalendarPage'

vi.mock('@fullcalendar/daygrid', () => ({ default: {} }))
vi.mock('@fullcalendar/react', () => ({
  default: ({ events }: { events: { id: string; title: string; start: string; backgroundColor: string }[] }) => (
    <ul aria-label="calendar events">
      {events.map(e => <li key={e.id} data-start={e.start} data-color={e.backgroundColor}>{e.title}</li>)}
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
})
