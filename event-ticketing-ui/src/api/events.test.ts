import { fetchEvents } from './events'

describe('fetchEvents', () => {
  afterEach(() => vi.unstubAllGlobals())

  function stub() {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}'))
    vi.stubGlobal('fetch', fetchMock)
    return fetchMock
  }

  it('gets the first page without query parameters by default', async () => {
    const fetchMock = stub()

    await fetchEvents()

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events')
  })

  it('passes search, sort and cursor as query parameters', async () => {
    const fetchMock = stub()

    await fetchEvents({ q: 'jazz night', sort: 'price', after: 'abc123' })

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events?q=jazz+night&sort=price&after=abc123')
  })

  it('omits empty search and missing cursor', async () => {
    const fetchMock = stub()

    await fetchEvents({ q: '', sort: 'date', after: null })

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events?sort=date')
  })
})
