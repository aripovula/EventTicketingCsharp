import { useCallback, useEffect, useRef, useState } from 'react'

/**
 * Loads a cursor-paginated list ({ data, meta.nextCursor }). Put sentinelRef on an element under the
 * list; when it scrolls into view the next page is appended. fetchPage must be a stable callback.
 */
export function useCursorPages<T>(fetchPage: (after: string | null) => Promise<Response>, enabled = true) {
  const [items, setItems] = useState<T[]>([])
  const [loading, setLoading] = useState(enabled)
  const [error, setError] = useState(false)
  const [nextCursor, setNextCursor] = useState<string | null>(null)
  const [loadingMore, setLoadingMore] = useState(false)
  const sentinelRef = useRef<HTMLDivElement>(null)

  const load = useCallback(async (after: string | null) => {
    const res = await fetchPage(after)
    if (!res.ok) throw new Error(`Failed to load (${res.status}).`)
    const body = await res.json()
    return { data: (body.data ?? []) as T[], next: (body.meta?.nextCursor ?? null) as string | null }
  }, [fetchPage])

  useEffect(() => {
    if (!enabled) return
    load(null)
      .then(({ data, next }) => { setItems(data); setNextCursor(next) })
      .catch(() => setError(true))
      .finally(() => setLoading(false))
  }, [load, enabled])

  const loadMore = useCallback(() => {
    if (!nextCursor || loadingMore) return
    setLoadingMore(true)
    load(nextCursor)
      .then(({ data, next }) => { setItems(prev => [...prev, ...data]); setNextCursor(next) })
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
