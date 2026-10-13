import { fetchMyPaymentMethod, saveMyPaymentMethod } from './paymentMethod'

describe('payment method API', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('reads the saved card with cookies', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}'))
    vi.stubGlobal('fetch', fetchMock)

    await fetchMyPaymentMethod()

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/auth/me/payment-method', { credentials: 'include' })
  })

  it('saves the card by its Stripe PaymentMethod id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}'))
    vi.stubGlobal('fetch', fetchMock)

    await saveMyPaymentMethod('pm_card_visa')

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/auth/me/payment-method', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ paymentMethodId: 'pm_card_visa' }),
    })
  })
})
