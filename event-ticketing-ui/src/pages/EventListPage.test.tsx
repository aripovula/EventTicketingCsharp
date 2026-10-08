import { render, screen } from '@testing-library/react'
import EventListPage from './EventListPage'

describe('EventListPage', () => {
  it('renders the events list', () => {
    render(<EventListPage />)

    expect(screen.getByRole('list', { name: 'events' })).toBeInTheDocument()
  })
})
