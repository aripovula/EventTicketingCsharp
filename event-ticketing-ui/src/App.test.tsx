import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from './App'
import { AuthProvider } from './context/AuthProvider'

function renderApp() {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })))
  render(<AuthProvider><App /></AuthProvider>)
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
})
