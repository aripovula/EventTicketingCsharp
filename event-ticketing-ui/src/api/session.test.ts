import { authFetch, refreshOnce } from './session'

const ok = () => new Response('{}')
const unauthorized = () => new Response(null, { status: 401 })

describe('authFetch', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('sends the cookies and returns any non-401 answer as it is', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 403 }))
    vi.stubGlobal('fetch', fetchMock)

    const res = await authFetch('/api/v1/admin/summary')

    expect(res.status).toBe(403)
    expect(fetchMock).toHaveBeenCalledExactlyOnceWith('/api/v1/admin/summary', { credentials: 'include' })
  })

  it('refreshes an expired session and retries the request once', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(unauthorized())
      .mockResolvedValueOnce(ok())
      .mockResolvedValueOnce(new Response('{"data":[]}'))
    vi.stubGlobal('fetch', fetchMock)

    const res = await authFetch('/api/v1/admin/summary')

    expect(await res.text()).toBe('{"data":[]}')
    expect(fetchMock.mock.calls.map(c => c[0])).toEqual(['/api/v1/admin/summary', '/api/v1/auth/refresh', '/api/v1/admin/summary'])
  })

  it('returns the 401 when the session cannot be refreshed', async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(unauthorized()).mockResolvedValueOnce(unauthorized())
    vi.stubGlobal('fetch', fetchMock)

    const res = await authFetch('/api/v1/admin/summary')

    expect(res.status).toBe(401)
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })
})

describe('refreshOnce', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('shares one refresh between callers that ask at the same moment', async () => {
    const fetchMock = vi.fn().mockResolvedValue(ok())
    vi.stubGlobal('fetch', fetchMock)

    expect(await Promise.all([refreshOnce(), refreshOnce(), refreshOnce()])).toEqual([true, true, true])
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('refreshes again once the earlier refresh has finished', async () => {
    const fetchMock = vi.fn().mockResolvedValue(ok())
    vi.stubGlobal('fetch', fetchMock)

    await refreshOnce()
    await refreshOnce()

    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('reports a failed refresh, including a network error', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('offline')))

    expect(await refreshOnce()).toBe(false)
  })
})
