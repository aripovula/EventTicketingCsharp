import { render, screen } from '@testing-library/react'
import MyOrdersPage from './MyOrdersPage'

describe('MyOrdersPage', () => {
  it('renders the my-orders heading', () => {
    render(<MyOrdersPage />)

    expect(screen.getByRole('heading', { name: 'My orders' })).toBeInTheDocument()
  })
})
