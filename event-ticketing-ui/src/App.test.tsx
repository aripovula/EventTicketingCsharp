import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from './App'
import { AuthProvider } from './context/AuthProvider'

const john = { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }

function renderApp(signedIn = false) {
  const fetchMock = vi.fn((url: string) => {
    if (url === '/api/v1/auth/me')
      return Promise.resolve(signedIn
        ? new Response(JSON.stringify({ data: john, errors: [] }))
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
})
