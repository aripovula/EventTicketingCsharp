import { fetchSummary } from './admin'

describe('fetchSummary', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('gets the admin summary with cookies included', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}'))
    vi.stubGlobal('fetch', fetchMock)

    await fetchSummary()

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/admin/summary', { credentials: 'include' })
  })
})
