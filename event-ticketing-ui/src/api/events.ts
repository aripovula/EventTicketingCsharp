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

export type EventInput = Omit<EventItem, 'id'>

export function createEvent(input: EventInput) {
  return fetch('/api/v1/events', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(input),
  })
}

export function updateEvent(id: number, input: EventInput) {
  return fetch(`/api/v1/events/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(input),
  })
}
