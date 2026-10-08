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
