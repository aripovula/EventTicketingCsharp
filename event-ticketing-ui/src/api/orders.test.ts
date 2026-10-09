import { fetchOrder } from './orders'

describe('fetchOrder', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('gets an order with cookies included', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}'))
    vi.stubGlobal('fetch', fetchMock)

    await fetchOrder(42)

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/orders/42', { credentials: 'include', headers: {} })
  })

  it('sends a guest token as the X-Order-Token header', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}'))
    vi.stubGlobal('fetch', fetchMock)

    await fetchOrder(42, 'secret-token')

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/orders/42', { credentials: 'include', headers: { 'X-Order-Token': 'secret-token' } })
  })
})
