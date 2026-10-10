import { useCallback, useEffect, useRef, useState } from 'react'

/**
 * Loads a cursor-paginated list ({ data, meta.nextCursor }). Put sentinelRef on an element under the
 * list; when it scrolls into view the next page is appended. fetchPage must be a stable callback;
 * passing a new one (e.g. for a new filter) starts over from the first page.
 */
export function useCursorPages<T>(fetchPage: (after: string | null) => Promise<Response>, enabled = true) {
  const [items, setItems] = useState<T[]>([])
  const [loading, setLoading] = useState(enabled)
  const [error, setError] = useState(false)
  const [nextCursor, setNextCursor] = useState<string | null>(null)
  const [loadingMore, setLoadingMore] = useState(false)
  const sentinelRef = useRef<HTMLDivElement>(null)
  // Bumped whenever fetchPage changes, so late responses for the old source are ignored.
  const requestId = useRef(0)

  // A new source starts over (React's "adjust state when a prop changes" pattern, no extra effect).
  const [source, setSource] = useState(() => fetchPage)
  if (source !== fetchPage) {
    setSource(() => fetchPage)
    setItems([])
    setNextCursor(null)
    setError(false)
    setLoading(enabled)
  }

  const load = useCallback(async (after: string | null) => {
    const res = await fetchPage(after)
    if (!res.ok) throw new Error(`Failed to load (${res.status}).`)
    const body = await res.json()
    return { data: (body.data ?? []) as T[], next: (body.meta?.nextCursor ?? null) as string | null }
  }, [fetchPage])

  useEffect(() => {
    if (!enabled) return
    const id = ++requestId.current
    load(null)
      .then(({ data, next }) => {
        if (id !== requestId.current) return
        setItems(data)
        setNextCursor(next)
      })
      .catch(() => { if (id === requestId.current) setError(true) })
      .finally(() => { if (id === requestId.current) setLoading(false) })
  }, [load, enabled])

  const loadMore = useCallback(() => {
    if (!nextCursor || loadingMore) return
    const id = requestId.current
    setLoadingMore(true)
    load(nextCursor)
      .then(({ data, next }) => {
        if (id !== requestId.current) return
        setItems(prev => [...prev, ...data])
        setNextCursor(next)
      })
      .catch(() => setError(true))
      .finally(() => setLoadingMore(false))
  }, [load, nextCursor, loadingMore])

  useEffect(() => {
    const sentinel = sentinelRef.current
    if (!sentinel) return
    const observer = new IntersectionObserver(entries => {
      if (entries.some(entry => entry.isIntersecting)) loadMore()
    })
    observer.observe(sentinel)
    return () => observer.disconnect()
  }, [loadMore, loading])

  return { items, loading, error, loadingMore, sentinelRef }
}
