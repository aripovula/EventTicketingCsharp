import { fetchMe, login, logout, refreshSession, register } from './auth'

describe('login', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('posts the credentials as JSON to the login endpoint with cookies included', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}'))
    vi.stubGlobal('fetch', fetchMock)

    await login('john@example.com', 'Password')

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ email: 'john@example.com', password: 'Password' }),
    })
  })
})

describe('fetchMe', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('gets the current user with cookies included', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}'))
    vi.stubGlobal('fetch', fetchMock)

    await fetchMe()

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/auth/me', { credentials: 'include' })
  })
})

describe('logout', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('posts to the logout endpoint with cookies included', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)

    await logout()

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/auth/logout', { method: 'POST', credentials: 'include' })
  })
})

describe('refreshSession', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('posts to the refresh endpoint with cookies included', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}'))
    vi.stubGlobal('fetch', fetchMock)

    await refreshSession()

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/auth/refresh', { method: 'POST', credentials: 'include' })
  })
})

describe('register', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('posts the new account as JSON to the register endpoint', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 201 }))
    vi.stubGlobal('fetch', fetchMock)

    await register('New User', 'new.user@example.com', 'secret-password')

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/auth/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ name: 'New User', email: 'new.user@example.com', password: 'secret-password' }),
    })
  })
})
