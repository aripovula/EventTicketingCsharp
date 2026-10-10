import { fetchAllMyOrders, fetchAllOrders, fetchMyOrders, fetchOrder } from './orders'
import { makeOrder } from '../test/orderFixtures'

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

describe('fetchMyOrders', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('gets the first page of the signed-in user orders', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}'))
    vi.stubGlobal('fetch', fetchMock)

    await fetchMyOrders()

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/auth/me/orders', { credentials: 'include' })
  })

  it('passes the cursor for later pages', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}'))
    vi.stubGlobal('fetch', fetchMock)

    await fetchMyOrders('abc+/=')

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/auth/me/orders?after=abc%2B%2F%3D', { credentials: 'include' })
  })
})

describe('fetchAllOrders', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('gets all orders for the admin, with an optional cursor', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}'))
    vi.stubGlobal('fetch', fetchMock)

    await fetchAllOrders()
    await fetchAllOrders('c1')

    expect(fetchMock).toHaveBeenNthCalledWith(1, '/api/v1/admin/orders', { credentials: 'include' })
    expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/v1/admin/orders?after=c1', { credentials: 'include' })
  })

  it('narrows the orders to one event', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}'))
    vi.stubGlobal('fetch', fetchMock)

    await fetchAllOrders(null, 7)
    await fetchAllOrders('c1', 7)

    expect(fetchMock).toHaveBeenNthCalledWith(1, '/api/v1/admin/orders?eventId=7', { credentials: 'include' })
    expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/v1/admin/orders?eventId=7&after=c1', { credentials: 'include' })
  })
})

describe('fetchAllMyOrders', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('follows the cursor through every page', async () => {
    const fetchMock = vi.fn<(url: string) => Promise<Response>>(url => Promise.resolve(new Response(JSON.stringify(
      url.includes('after=c1')
        ? { data: [makeOrder({ id: 2 })], errors: [], meta: { nextCursor: null } }
        : { data: [makeOrder({ id: 1 })], errors: [], meta: { nextCursor: 'c1' } }))))
    vi.stubGlobal('fetch', fetchMock)

    expect((await fetchAllMyOrders()).map(o => o.id)).toEqual([1, 2])
  })

  it('fails when a page cannot be loaded', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 500 })))

    await expect(fetchAllMyOrders()).rejects.toThrow('Failed to load orders (500).')
  })
})
