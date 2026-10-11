import { bookEvent } from './bookings'

describe('bookEvent', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('posts the attendee email with its idempotency key and cookies', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 201 }))
    vi.stubGlobal('fetch', fetchMock)

    await bookEvent(7, 'john@example.com', 'key-1')

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events/7/bookings', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Idempotency-Key': 'key-1' },
      credentials: 'include',
      body: JSON.stringify({ email: 'john@example.com' }),
    })
  })
})
