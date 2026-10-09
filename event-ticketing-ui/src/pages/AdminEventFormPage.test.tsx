import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import AdminEventFormPage from './AdminEventFormPage'

describe('AdminEventFormPage', () => {
  it('renders the new-event heading and a link back to admin', () => {
    render(<MemoryRouter><AdminEventFormPage /></MemoryRouter>)

    expect(screen.getByRole('heading', { name: 'New event' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '← Back to admin' })).toHaveAttribute('href', '/admin')
  })
})
