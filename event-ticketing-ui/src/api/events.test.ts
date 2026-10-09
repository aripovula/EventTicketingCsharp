import { createEvent, fetchEvent, fetchEvents, type EventInput } from './events'
import { makeEvent } from '../test/fixtures'

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

describe('fetchEvent', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('gets a single event by id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}'))
    vi.stubGlobal('fetch', fetchMock)

    await fetchEvent(7)

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events/7')
  })
})

describe('createEvent', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('posts the new event as JSON with cookies included', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 201 }))
    vi.stubGlobal('fetch', fetchMock)
    const input = Object.fromEntries(Object.entries(makeEvent()).filter(([key]) => key !== 'id')) as EventInput

    await createEvent(input)

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(input),
    })
  })
})
