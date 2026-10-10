import { render, screen } from '@testing-library/react'
import MyCalendarPage from './MyCalendarPage'

describe('MyCalendarPage', () => {
  it('renders the My Calendar heading', () => {
    render(<MyCalendarPage />)

    expect(screen.getByRole('heading', { name: 'My Calendar' })).toBeInTheDocument()
  })
})
