import { fetchMyPaymentMethod } from './paymentMethod'

describe('payment method API', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('reads the saved card with cookies', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}'))
    vi.stubGlobal('fetch', fetchMock)

    await fetchMyPaymentMethod()

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/auth/me/payment-method', { credentials: 'include' })
  })
})
