import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthContext, type User } from '../context/authContext'
import MyCalendarPage from './MyCalendarPage'

const john: User = { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }

function renderAs(user: User | null) {
  render(
    <AuthContext.Provider value={{ user, loading: false, signIn: () => {}, signOut: async () => {} }}>
      <MemoryRouter initialEntries={['/calendar']}>
        <Routes>
          <Route path="/login" element={<p>Login page</p>} />
          <Route path="/calendar" element={<MyCalendarPage />} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

describe('MyCalendarPage', () => {
  it('renders the My Calendar heading for a signed-in user', () => {
    renderAs(john)

    expect(screen.getByRole('heading', { name: 'My Calendar' })).toBeInTheDocument()
  })

  it('sends a signed-out visitor to the login page', () => {
    renderAs(null)

    expect(screen.getByText('Login page')).toBeInTheDocument()
  })
})
