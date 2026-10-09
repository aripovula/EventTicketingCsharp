export interface EventSummary {
  eventId: number
  title: string
  totalSeats: number
  soldSeats: number
  remainingSeats: number
  revenueCents: number
}

export function fetchSummary() {
  return fetch('/api/v1/admin/summary', { credentials: 'include' })
}
