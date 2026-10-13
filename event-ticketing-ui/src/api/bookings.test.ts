import { bookEvent } from './bookings'

describe('bookEvent', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('posts the attendee email and card with its idempotency key and cookies', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 201 }))
    vi.stubGlobal('fetch', fetchMock)

    await bookEvent(7, 'john@example.com', { paymentMethodId: 'pm_card_visa' }, 'key-1')

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events/7/bookings', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Idempotency-Key': 'key-1' },
      credentials: 'include',
      body: JSON.stringify({ email: 'john@example.com', paymentMethodId: 'pm_card_visa' }),
    })
  })

  it('asks for the saved card instead of sending one', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 201 }))
    vi.stubGlobal('fetch', fetchMock)

    await bookEvent(7, 'john@example.com', { useSavedPaymentMethod: true }, 'key-1')

    expect(fetchMock.mock.calls[0][1].body).toBe(JSON.stringify({ email: 'john@example.com', useSavedPaymentMethod: true }))
  })

  it('sends no card for a free event', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 201 }))
    vi.stubGlobal('fetch', fetchMock)

    await bookEvent(7, 'john@example.com', null, 'key-1')

    expect(fetchMock.mock.calls[0][1].body).toBe(JSON.stringify({ email: 'john@example.com' }))
  })
})
