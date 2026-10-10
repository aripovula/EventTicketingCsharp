import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import OrderConfirmationPage from './OrderConfirmationPage'

describe('OrderConfirmationPage', () => {
  it('links back to browsing events', () => {
    render(<MemoryRouter><OrderConfirmationPage /></MemoryRouter>)

    expect(screen.getByRole('link', { name: '← Browse more events' })).toHaveAttribute('href', '/')
  })
})
