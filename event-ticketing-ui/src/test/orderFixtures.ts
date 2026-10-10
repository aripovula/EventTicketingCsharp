import type { OrderItem } from '../api/orders'

export function makeOrder(overrides: Partial<OrderItem> = {}): OrderItem {
  return {
    id: 42,
    eventId: 1,
    eventTitle: 'Jazz Night',
    eventVenue: 'Blue Note Club',
    eventStartTime: '2026-11-02T23:00:00Z',
    eventEndTime: '2026-11-03T02:00:00Z',
    email: 'john@example.com',
    priceCents: 2500,
    bookedAt: '2026-10-09T12:00:00Z',
    ...overrides,
  }
}
