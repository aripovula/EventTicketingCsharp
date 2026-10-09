import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import AdminPage from './AdminPage'

describe('AdminPage', () => {
  it('renders the admin heading and a new-event link', () => {
    render(<MemoryRouter><AdminPage /></MemoryRouter>)

    expect(screen.getByRole('heading', { name: 'Admin' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '+ New event' })).toHaveAttribute('href', '/admin/new')
  })
})
