import { authFetch } from './session'

export interface EventItem {
  id: number
  title: string
  description: string
  startTime: string
  endTime: string
  venue: string
  eventType: string
  totalSeats: number
  availableSeats: number
  priceCents: number
  imageUrl?: string | null
  /** Events with orders can't be deleted, only archived. */
  hasOrders?: boolean
  /** Set once an admin archived the event: it no longer sells tickets. */
  archivedAt?: string | null
}

export type EventSort = 'name' | 'date' | 'price'

export interface EventListParams {
  q?: string
  sort?: EventSort
  after?: string | null
}

export function fetchEvents({ q, sort, after }: EventListParams = {}) {
  const params = new URLSearchParams()
  if (q) params.set('q', q)
  if (sort) params.set('sort', sort)
  if (after) params.set('after', after)
  const query = params.toString()
  return fetch(query ? `/api/v1/events?${query}` : '/api/v1/events')
}

export function fetchEvent(id: number) {
  return fetch(`/api/v1/events/${id}`)
}

export type EventInput = Omit<EventItem, 'id' | 'hasOrders' | 'archivedAt'>

export function createEvent(input: EventInput) {
  return authFetch('/api/v1/events', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(input),
  })
}

export function updateEvent(id: number, input: EventInput) {
  return authFetch(`/api/v1/events/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(input),
  })
}

export function deleteEvent(id: number) {
  return authFetch(`/api/v1/events/${id}`, { method: 'DELETE', credentials: 'include' })
}

export function archiveEvent(id: number) {
  return authFetch(`/api/v1/events/${id}/archive`, { method: 'POST', credentials: 'include' })
}

/** Every event in date order, following the cursor through all pages (for the admin calendar). */
export async function fetchAllEvents(): Promise<EventItem[]> {
  const all: EventItem[] = []
  let after: string | null = null
  do {
    const res = await fetchEvents({ sort: 'date', after })
    if (!res.ok) throw new Error(`Failed to load events (${res.status}).`)
    const body = await res.json()
    all.push(...(body.data ?? []))
    after = body.meta?.nextCursor ?? null
  } while (after)
  return all
}
