import { login } from './auth'

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
