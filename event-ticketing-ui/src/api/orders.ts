export interface OrderItem {
  id: number
  eventId: number
  eventTitle: string
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
