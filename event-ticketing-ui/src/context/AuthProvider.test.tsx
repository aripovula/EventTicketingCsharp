import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { AuthProvider } from './AuthProvider'
import { useAuth } from './useAuth'

const jane = { userId: 2, name: 'Jane Doer', email: 'jane@example.com', role: 'user' as const }

function Probe() {
  const { user, signIn, signOut } = useAuth()
  return (
    <>
      <p>{user ? `Signed in as ${user.name}` : 'Signed out'}</p>
      <button onClick={() => signIn(jane)}>sign in</button>
      <button onClick={() => signOut().catch(() => {})}>sign out</button>
    </>
  )
}

describe('AuthProvider', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('restores the signed-in user from /me on mount', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: jane, errors: [] })))
    vi.stubGlobal('fetch', fetchMock)

    render(<AuthProvider><Probe /></AuthProvider>)

    expect(await screen.findByText('Signed in as Jane Doer')).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/auth/me', { credentials: 'include' })
  })

  it('stays signed out when /me returns 401', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })))

    render(<AuthProvider><Probe /></AuthProvider>)
    await act(async () => {})

    expect(screen.getByText('Signed out')).toBeInTheDocument()
  })

  it('stays signed out when /me cannot be reached', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    render(<AuthProvider><Probe /></AuthProvider>)
    await act(async () => {})

    expect(screen.getByText('Signed out')).toBeInTheDocument()
  })

  it('signIn sets the current user', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })))
    render(<AuthProvider><Probe /></AuthProvider>)

    await userEvent.click(screen.getByRole('button', { name: 'sign in' }))

    expect(screen.getByText('Signed in as Jane Doer')).toBeInTheDocument()
  })

  it('useAuth outside a provider throws a clear error', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})

    expect(() => render(<Probe />)).toThrow('useAuth must be used inside an AuthProvider')
  })

  it('signOut calls the logout endpoint and clears the user', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: jane, errors: [] })))
    vi.stubGlobal('fetch', fetchMock)
    render(<AuthProvider><Probe /></AuthProvider>)
    await screen.findByText('Signed in as Jane Doer')

    await userEvent.click(screen.getByRole('button', { name: 'sign out' }))

    expect(screen.getByText('Signed out')).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/auth/logout', { method: 'POST', credentials: 'include' })
  })

  it('signOut clears the user even when the logout request fails', async () => {
    vi.stubGlobal('fetch', vi.fn((url: string) =>
      url === '/api/v1/auth/me'
        ? Promise.resolve(new Response(JSON.stringify({ data: jane, errors: [] })))
        : Promise.reject(new TypeError('Failed to fetch'))))
    render(<AuthProvider><Probe /></AuthProvider>)
    await screen.findByText('Signed in as Jane Doer')

    await userEvent.click(screen.getByRole('button', { name: 'sign out' }))

    expect(await screen.findByText('Signed out')).toBeInTheDocument()
  })
})
