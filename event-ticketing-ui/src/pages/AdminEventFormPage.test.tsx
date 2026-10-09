import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import AdminEventFormPage from './AdminEventFormPage'

function renderPage() {
  render(<MemoryRouter><AdminEventFormPage /></MemoryRouter>)
}

describe('AdminEventFormPage', () => {
  it('renders the new-event heading and a link back to admin', () => {
    renderPage()

    expect(screen.getByRole('heading', { name: 'New event' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '← Back to admin' })).toHaveAttribute('href', '/admin')
  })

  it('has every event field', () => {
    renderPage()

    for (const label of ['Title', 'Venue', 'Description', 'Image URL', 'Start time', 'End time',
      'Event type', 'Total seats', 'Available seats', 'Price ($)']) {
      expect(screen.getByLabelText(label)).toBeInTheDocument()
    }
  })

  it('offers every event type, defaulting to Music', () => {
    renderPage()

    const select = screen.getByLabelText('Event type')
    expect(select).toHaveValue('Music')
    expect(screen.getAllByRole('option')).toHaveLength(9)
  })

  it('keeps what the admin types', async () => {
    renderPage()

    await userEvent.type(screen.getByLabelText('Title'), 'Brand New Gig')
    await userEvent.type(screen.getByLabelText('Price ($)'), '19.99')

    expect(screen.getByLabelText('Title')).toHaveValue('Brand New Gig')
    expect(screen.getByLabelText('Price ($)')).toHaveValue(19.99)
  })

  it('offers create and cancel actions', () => {
    renderPage()

    expect(screen.getByRole('button', { name: 'Create event' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Cancel' })).toHaveAttribute('href', '/admin')
  })
})
