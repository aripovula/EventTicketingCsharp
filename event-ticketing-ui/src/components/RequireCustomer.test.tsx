import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthContext, type User } from '../context/authContext'
import RequireCustomer from './RequireCustomer'

const admin: User = { userId: 4, name: 'Admin', email: 'admin@example.com', role: 'admin' }
const john: User = { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }

function renderAt(user: User | null, loading = false) {
  render(
    <AuthContext.Provider value={{ user, loading, signIn: () => {}, signOut: async () => {} }}>
      <MemoryRouter initialEntries={['/orders']}>
        <Routes>
          <Route path="/admin" element={<p>Admin area</p>} />
          <Route element={<RequireCustomer />}>
            <Route path="/orders" element={<p>My orders page</p>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

describe('RequireCustomer', () => {
  it('shows the page to a regular user', () => {
    renderAt(john)

    expect(screen.getByText('My orders page')).toBeInTheDocument()
  })

  it('shows the page to a signed-out visitor', () => {
    renderAt(null)

    expect(screen.getByText('My orders page')).toBeInTheDocument()
  })

  it('sends an admin to the admin area', () => {
    renderAt(admin)

    expect(screen.getByText('Admin area')).toBeInTheDocument()
    expect(screen.queryByText('My orders page')).not.toBeInTheDocument()
  })

  it('waits for the session check before deciding', () => {
    renderAt(null, true)

    expect(screen.queryByText('My orders page')).not.toBeInTheDocument()
    expect(screen.queryByText('Admin area')).not.toBeInTheDocument()
  })
})
