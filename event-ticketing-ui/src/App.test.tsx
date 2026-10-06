import { render, screen } from '@testing-library/react'
import App from './App'

describe('App', () => {
  it('shows the app title in the header', () => {
    render(<App />)

    expect(screen.getByRole('banner')).toHaveTextContent('Event Ticketing')
  })

  it('renders a main content area', () => {
    render(<App />)

    expect(screen.getByRole('main')).toBeInTheDocument()
  })
})
