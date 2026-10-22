import { authFetch } from './session'

export interface EventSummary {
  eventId: number
  title: string
  totalSeats: number
  soldSeats: number
  remainingSeats: number
  revenueCents: number
}

export function fetchSummary() {
  return authFetch('/api/v1/admin/summary', { credentials: 'include' })
}
