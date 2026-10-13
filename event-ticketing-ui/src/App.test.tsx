import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from './App'
import { AuthProvider } from './context/AuthProvider'

const john = { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }

const admin = { userId: 4, name: 'Admin', email: 'admin@example.com', role: 'admin' }

function renderApp(signedIn: boolean | typeof john = false) {
  const fetchMock = vi.fn((url: string) => {
    if (url === '/api/v1/auth/me')
      return Promise.resolve(signedIn
        ? new Response(JSON.stringify({ data: signedIn === true ? john : signedIn, errors: [] }))
        : new Response(null, { status: 401 }))
    return Promise.resolve(new Response(null, { status: 204 }))
  })
  vi.stubGlobal('fetch', fetchMock)
  render(<AuthProvider><App /></AuthProvider>)
  return fetchMock
}

describe('App', () => {
  afterEach(() => {
    window.history.pushState({}, '', '/')
    vi.unstubAllGlobals()
  })

  it('shows the app title in the header', () => {
    renderApp()

    expect(screen.getByRole('banner')).toHaveTextContent('Event Ticketing')
  })

  it('renders a main content area', () => {
    renderApp()

    expect(screen.getByRole('main')).toBeInTheDocument()
  })

  it('navigates to the login page from the Sign in link', async () => {
    renderApp()

    await userEvent.click(screen.getByRole('link', { name: 'Sign in' }))

    expect(screen.getByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
    expect(window.location.pathname).toBe('/login')
  })

  it('shows Sign out instead of Sign in when signed in', async () => {
    renderApp(true)

    expect(await screen.findByRole('button', { name: 'Sign out' })).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Sign in' })).not.toBeInTheDocument()
  })

  it('signing out calls logout and goes to the login page', async () => {
    const fetchMock = renderApp(true)

    await userEvent.click(await screen.findByRole('button', { name: 'Sign out' }))

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/auth/logout', { method: 'POST', credentials: 'include' })
    expect(window.location.pathname).toBe('/login')
    expect(screen.getByRole('link', { name: 'Sign in' })).toBeInTheDocument()
  })

  it('shows the events list on the home page', async () => {
    renderApp()

    expect(await screen.findByRole('list', { name: 'events' })).toBeInTheDocument()
  })

  it('shows the Admin link only to admins', async () => {
    renderApp(admin)

    expect(await screen.findByRole('link', { name: 'Admin' })).toHaveAttribute('href', '/admin')
  })

  it('hides the Admin link from regular users', async () => {
    renderApp(true)

    await screen.findByRole('button', { name: 'Sign out' })
    expect(screen.queryByRole('link', { name: 'Admin' })).not.toBeInTheDocument()
  })

  it('links to My orders from the header', () => {
    renderApp()

    expect(screen.getByRole('banner').querySelector('a[href="/orders"]')).toHaveTextContent('My orders')
  })

  it('links regular users to My Calendar', async () => {
    renderApp(true)

    expect(await screen.findByRole('link', { name: 'My Calendar' })).toHaveAttribute('href', '/calendar')
  })

  it('links admins to the events calendar instead', async () => {
    renderApp(admin)

    expect(await screen.findByRole('link', { name: 'Events Calendar' })).toHaveAttribute('href', '/admin/calendar')
    expect(screen.queryByRole('link', { name: 'My Calendar' })).not.toBeInTheDocument()
  })

  it('links signed-in users to their profile', async () => {
    renderApp(true)

    expect(await screen.findByRole('link', { name: 'Profile' })).toHaveAttribute('href', '/profile')
  })

  it('hides the Profile link from signed-out visitors', () => {
    renderApp()

    expect(screen.queryByRole('link', { name: 'Profile' })).not.toBeInTheDocument()
  })
})
