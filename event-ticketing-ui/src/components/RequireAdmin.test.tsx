import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthContext, type User } from '../context/authContext'
import RequireAdmin from './RequireAdmin'

const admin: User = { userId: 4, name: 'Admin', email: 'admin@example.com', role: 'admin' }
const john: User = { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }

function renderAt(user: User | null, loading = false) {
  render(
    <AuthContext.Provider value={{ user, loading, signIn: () => {}, signOut: async () => {} }}>
      <MemoryRouter initialEntries={['/admin']}>
        <Routes>
          <Route path="/login" element={<p>Login page</p>} />
          <Route element={<RequireAdmin />}>
            <Route path="/admin" element={<p>Admin area</p>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

describe('RequireAdmin', () => {
  it('shows the admin page to an admin', () => {
    renderAt(admin)

    expect(screen.getByText('Admin area')).toBeInTheDocument()
  })

  it('sends a signed-out visitor to the login page', () => {
    renderAt(null)

    expect(screen.getByText('Login page')).toBeInTheDocument()
  })

  it('tells a regular user they lack permission', () => {
    renderAt(john)

    expect(screen.getByText('You do not have permission to view this page.')).toBeInTheDocument()
    expect(screen.queryByText('Admin area')).not.toBeInTheDocument()
  })

  it('waits for the session check instead of redirecting early', () => {
    renderAt(null, true)

    expect(screen.queryByText('Login page')).not.toBeInTheDocument()
    expect(screen.queryByText('Admin area')).not.toBeInTheDocument()
  })
})
