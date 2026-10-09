import { useCallback, useEffect, useRef, useState } from 'react'
import { fetchEvents, type EventItem, type EventSort } from '../api/events'

/**
 * Loads events page by page from the cursor-paginated API. The returned sentinelRef
 * goes on an element under the list; when it scrolls into view the next page is appended.
 */
export function useEventPages({ q = '', sort }: { q?: string; sort: EventSort }) {
  const [events, setEvents] = useState<EventItem[]>([])
  const [loading, setLoading] = useState(true)
  const [nextCursor, setNextCursor] = useState<string | null>(null)
  const [loadingMore, setLoadingMore] = useState(false)
  const sentinelRef = useRef<HTMLDivElement>(null)
  // Bumped whenever search or sort changes, so late responses for old criteria are ignored.
  const requestId = useRef(0)

  useEffect(() => {
    const id = ++requestId.current
    fetchEvents({ q, sort })
      .then(res => res.json())
      .then(body => {
        if (id !== requestId.current) return
        setEvents(body.data ?? [])
        setNextCursor(body.meta?.nextCursor ?? null)
      })
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [q, sort])

  const loadMore = useCallback(() => {
    if (!nextCursor || loadingMore) return
    const id = requestId.current
    setLoadingMore(true)
    fetchEvents({ q, sort, after: nextCursor })
      .then(res => res.json())
      .then(body => {
        if (id !== requestId.current) return
        setEvents(prev => [...prev, ...(body.data ?? [])])
        setNextCursor(body.meta?.nextCursor ?? null)
      })
      .catch(() => {})
      .finally(() => setLoadingMore(false))
  }, [nextCursor, loadingMore, q, sort])

  // Infinite scroll: load the next page when the sentinel under the list scrolls into view.
  useEffect(() => {
    const sentinel = sentinelRef.current
    if (!sentinel) return
    const observer = new IntersectionObserver(entries => {
      if (entries.some(entry => entry.isIntersecting)) loadMore()
    })
    observer.observe(sentinel)
    return () => observer.disconnect()
  }, [loadMore, loading])

  return { events, setEvents, loading, loadingMore, sentinelRef }
}
