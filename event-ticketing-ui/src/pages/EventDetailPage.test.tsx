import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import EventDetailPage from './EventDetailPage'

describe('EventDetailPage', () => {
  it('links back to the events list', () => {
    render(<MemoryRouter><EventDetailPage /></MemoryRouter>)

    expect(screen.getByRole('link', { name: '← Back to events' })).toHaveAttribute('href', '/')
  })
})
