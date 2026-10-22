import { authFetch } from './session'

export interface OrderItem {
  id: number
  eventId: number
  eventTitle: string
  eventVenue: string
  eventType: string
  eventStartTime: string
  eventEndTime: string
  email: string
  priceCents: number
  bookedAt: string
  guestAccessToken?: string | null
}

export function fetchOrder(id: number, guestToken?: string | null) {
  return fetch(`/api/v1/orders/${id}`, {
    credentials: 'include',
    headers: guestToken ? { 'X-Order-Token': guestToken } : {},
  })
}

export function fetchMyOrders(after?: string | null) {
  return authFetch(after ? `/api/v1/auth/me/orders?after=${encodeURIComponent(after)}` : '/api/v1/auth/me/orders', {
    credentials: 'include',
  })
}

export function fetchAllOrders(after?: string | null, eventId?: number | null) {
  const params = new URLSearchParams()
  if (eventId) params.set('eventId', String(eventId))
  if (after) params.set('after', after)
  const query = params.toString()
  return authFetch(query ? `/api/v1/admin/orders?${query}` : '/api/v1/admin/orders', { credentials: 'include' })
}

/** Every order of the signed-in user, following the cursor through all pages (for My Calendar). */
export async function fetchAllMyOrders(): Promise<OrderItem[]> {
  const all: OrderItem[] = []
  let after: string | null = null
  do {
    const res = await fetchMyOrders(after)
    if (!res.ok) throw new Error(`Failed to load orders (${res.status}).`)
    const body = await res.json()
    all.push(...(body.data ?? []))
    after = body.meta?.nextCursor ?? null
  } while (after)
  return all
}
