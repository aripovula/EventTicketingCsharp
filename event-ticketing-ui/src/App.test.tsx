import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from './App'

describe('App', () => {
  afterEach(() => window.history.pushState({}, '', '/'))

  it('shows the app title in the header', () => {
    render(<App />)

    expect(screen.getByRole('banner')).toHaveTextContent('Event Ticketing')
  })

  it('renders a main content area', () => {
    render(<App />)

    expect(screen.getByRole('main')).toBeInTheDocument()
  })

  it('navigates to the login page from the Sign in link', async () => {
    render(<App />)

    await userEvent.click(screen.getByRole('link', { name: 'Sign in' }))

    expect(screen.getByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
    expect(window.location.pathname).toBe('/login')
  })
})
