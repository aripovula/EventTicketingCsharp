import type { EventItem } from '../api/events'

export function makeEvent(overrides: Partial<EventItem> = {}): EventItem {
  return {
    id: 1,
    title: 'Jazz Night',
    description: 'Live jazz.',
    startTime: '2026-11-02T23:00:00Z',
    endTime: '2026-11-03T02:00:00Z',
    venue: 'Blue Note Club',
    eventType: 'Music',
    totalSeats: 120,
    availableSeats: 48,
    priceCents: 2500,
    imageUrl: 'https://example.com/jazz.jpg',
    ...overrides,
  }
}
