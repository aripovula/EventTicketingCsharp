import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { AuthContext, type User } from '../context/authContext'
import ProfilePage from './ProfilePage'

const john: User = { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }

const visa = { brand: 'visa', last4: '4242', expMonth: 12, expYear: 2029 }

function renderPage(user: User | null = john) {
  render(
    <AuthContext.Provider value={{ user, loading: false, signIn: () => {}, signOut: async () => {} }}>
      <MemoryRouter><ProfilePage /></MemoryRouter>
    </AuthContext.Provider>,
  )
}

const json = (body: unknown, status = 200) => Promise.resolve(new Response(JSON.stringify(body), { status }))

function stubApi({ saved = null as typeof visa | null } = {}) {
  const fetchMock = vi.fn(() => saved ? json({ data: saved, errors: [] }) : json({ data: null, errors: [] }, 404))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

describe('ProfilePage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('asks signed-out visitors to sign in', () => {
    stubApi()
    renderPage(null)

    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/login')
  })

  it('shows the account name and email', () => {
    stubApi()
    renderPage()

    expect(screen.getByText('John Doe')).toBeInTheDocument()
    expect(screen.getByText('john@example.com')).toBeInTheDocument()
  })

  it('shows the saved card', async () => {
    stubApi({ saved: visa })
    renderPage()

    expect(await screen.findByText('Visa ending in 4242')).toBeInTheDocument()
    expect(screen.getByText('Expires 12/2029')).toBeInTheDocument()
  })

  it('says when no card is saved', async () => {
    stubApi()
    renderPage()

    expect(await screen.findByText('No saved card yet.')).toBeInTheDocument()
  })
})
