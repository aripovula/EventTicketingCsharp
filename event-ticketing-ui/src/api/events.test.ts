import { archiveEvent, createEvent, deleteEvent, fetchAllEvents, fetchEvent, fetchEvents, updateEvent, type EventInput } from './events'
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

describe('updateEvent', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('puts the edited event as JSON with cookies included', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)
    const input = Object.fromEntries(Object.entries(makeEvent()).filter(([key]) => key !== 'id')) as EventInput

    await updateEvent(7, input)

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events/7', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(input),
    })
  })
})

describe('deleteEvent', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('deletes the event with cookies included', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)

    await deleteEvent(7)

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events/7', { method: 'DELETE', credentials: 'include' })
  })
})

describe('archiveEvent', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('archives the event with cookies included', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)

    await archiveEvent(7)

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events/7/archive', { method: 'POST', credentials: 'include' })
  })
})

describe('fetchAllEvents', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('follows the cursor through every page in date order', async () => {
    const fetchMock = vi.fn<(url: string) => Promise<Response>>(url => Promise.resolve(new Response(JSON.stringify(
      url.includes('after=c1')
        ? { data: [makeEvent({ id: 2 })], errors: [], meta: { nextCursor: null } }
        : { data: [makeEvent({ id: 1 })], errors: [], meta: { nextCursor: 'c1' } }))))
    vi.stubGlobal('fetch', fetchMock)

    const events = await fetchAllEvents()

    expect(events.map(e => e.id)).toEqual([1, 2])
    expect(fetchMock.mock.calls.map(([url]) => url)).toEqual(['/api/v1/events?sort=date', '/api/v1/events?sort=date&after=c1'])
  })

  it('fails when a page cannot be loaded', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 500 })))

    await expect(fetchAllEvents()).rejects.toThrow('Failed to load events (500).')
  })
})
