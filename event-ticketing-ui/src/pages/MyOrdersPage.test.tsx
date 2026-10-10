import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { AuthContext, type User } from '../context/authContext'
import MyOrdersPage from './MyOrdersPage'

function renderWithUser(ui: React.ReactNode, user: User | null, loading = false) {
  render(
    <AuthContext.Provider value={{ user, loading, signIn: () => {}, signOut: async () => {} }}>
      <MemoryRouter>{ui}</MemoryRouter>
    </AuthContext.Provider>,
  )
}

describe('MyOrdersPage', () => {
  it('renders the my-orders heading', () => {
    renderWithUser(<MyOrdersPage />, null)

    expect(screen.getByRole('heading', { name: 'My orders' })).toBeInTheDocument()
  })

  it('asks signed-out visitors to sign in', () => {
    renderWithUser(<MyOrdersPage />, null)

    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/login')
  })

  it('waits for the session check', () => {
    renderWithUser(<MyOrdersPage />, null, true)

    expect(screen.queryByRole('heading', { name: 'My orders' })).not.toBeInTheDocument()
  })
})
