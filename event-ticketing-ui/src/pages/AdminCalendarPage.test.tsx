import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import AdminCalendarPage from './AdminCalendarPage'

describe('AdminCalendarPage', () => {
  it('renders the heading and a link back to admin', () => {
    render(<MemoryRouter><AdminCalendarPage /></MemoryRouter>)

    expect(screen.getByRole('heading', { name: 'Events Calendar' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '← Back to admin' })).toHaveAttribute('href', '/admin')
  })

  it('shows a colour legend for every event type except Other', () => {
    render(<MemoryRouter><AdminCalendarPage /></MemoryRouter>)

    const legend = within(screen.getByRole('list', { name: 'event type legend' }))
    expect(legend.getAllByRole('listitem')).toHaveLength(8)
    expect(legend.getByText('Music')).toBeInTheDocument()
    expect(legend.queryByText('Other')).not.toBeInTheDocument()
  })
})
