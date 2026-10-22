import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { fetchEvent, type EventSort } from '../api/events'
import { useEventPages } from '../hooks/useEventPages'
import { useHubEvents } from '../hooks/useHubEvents'
import { colorForType } from '../utils/eventTypes'
import { formatPrice } from '../utils/formatPrice'
import TechNote from '../components/TechNote'

const time = (iso: string) => new Date(iso).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })

export default function EventListPage() {
  const [searchTerm, setSearchTerm] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [sortType, setSortType] = useState<EventSort>('name')
  const searchInputRef = useRef<HTMLInputElement>(null)
  const { events, setEvents, reload, loading, loadingMore, sentinelRef } = useEventPages({ q: debouncedSearch, sort: sortType })

  // Search runs on the server, so wait for a pause in typing before asking for it.
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchTerm), 300)
    return () => clearTimeout(timer)
  }, [searchTerm])

  // Live updates patch the loaded cards in place, so pages already scrolled into view stay put.
  function refreshCard(eventId: number) {
    if (!events.some(e => e.id === eventId)) return
    fetchEvent(eventId)
      .then(res => (res.ok ? res.json() : null))
      .then(body => { if (body?.data) setEvents(prev => prev.map(e => (e.id === eventId ? body.data : e))) })
      .catch(() => {})
  }
  // A new event could belong anywhere in the current search and sort, so start the list over.
  useHubEvents({
    EventCreated: reload,
    BookingMade: refreshCard,
    EventUpdated: refreshCard,
    EventDeleted: eventId => setEvents(prev => prev.filter(e => e.id !== eventId)),
  })

  const changeInputSize = (inputWidth: number, fontSize: number) => {
    searchInputRef.current!.style.width = `${inputWidth}px`
    searchInputRef.current!.style.fontSize = `${fontSize}px`
  }

  if (loading) return <p className="text-gray-500 text-center py-12">Loading events...</p>

  return (
    <div>
      <TechNote points={[
        <><strong>Live updates (SignalR over WebSockets):</strong> a new event appears the moment an admin adds it, seat counts and edits change in place, and deleted events disappear, all without a reload.</>,
        <><strong>Infinite scroll with cursor pagination:</strong> the next 20 events load as you scroll, fetched &ldquo;after&rdquo; the last one shown rather than by page number, so new events never shift what you have already seen.</>,
        <><strong>Redis cache</strong> (an in-memory store shared by every API instance): list pages are cached through .NET HybridCache, so most visits are answered without touching the database, which keeps the list fast under heavy traffic. Any change to an event evicts them at once, so nobody sees stale data.</>,
        <><strong>Search in PostgreSQL</strong> (a case-insensitive <code>ILIKE</code> match on the title, done by the database rather than in the browser, so it works across every event, not just the ones loaded): it runs 300 ms after you stop typing, instead of on every keystroke.</>,
      ]} />
      <div className="flex gap-3 mb-6">
        <input
          ref={searchInputRef}
          type="text"
          aria-label="Search events"
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
          onFocus={() => changeInputSize(400, 22)}
          onBlur={() => changeInputSize(200, 12)}
          placeholder="type search term"
          style={{ width: '200px', fontSize: '12px' }}
          className="rounded-lg border border-gray-300 px-4 py-2 focus:outline-none focus:ring-2 focus:ring-cyan-400 transition"
        />
        <select
          aria-label="Sort events"
          value={sortType}
          onChange={e => setSortType(e.target.value as EventSort)}
          className="rounded-lg border border-gray-300 px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-cyan-400 transition"
        >
          <option value="name">Sort by name</option>
          <option value="date">Sort by date</option>
          <option value="price">Sort by price</option>
        </select>
      </div>

      <ul aria-label="events" className="space-y-4 list-none p-0 m-0">
        {events.map(event => (
          <li key={event.id} className="bg-white rounded-xl border border-gray-200 overflow-hidden hover:shadow-md transition-shadow flex h-[220px]">
            {event.imageUrl && (
              <img src={event.imageUrl} alt={event.title} className="aspect-[4/3] h-full object-cover shrink-0" />
            )}
            <div className="flex flex-col justify-between p-5 flex-1 min-w-0">
              <div>
                <span
                  className="inline-block text-white text-xs font-medium px-2 py-0.5 rounded-full mb-1"
                  style={{ backgroundColor: colorForType(event.eventType) }}
                >
                  {event.eventType}
                </span>
                <h2 className="text-xl font-semibold text-gray-900 mb-1 mt-0">
                  <Link to={`/events/${event.id}`} className="hover:text-cyan-600 transition-colors no-underline">
                    {event.title}
                  </Link>
                </h2>
                <p className="text-base text-gray-500">
                  {event.venue} · {new Date(event.startTime).toLocaleDateString(undefined, { dateStyle: 'medium' })}
                  {' '}{time(event.startTime)}{' – '}{time(event.endTime)}
                </p>
              </div>
              <div className="flex items-end justify-between">
                <p className="text-sm text-gray-400">{event.availableSeats} seats available</p>
                <div className="text-right shrink-0">
                  <span className="text-xl font-bold text-cyan-600">{formatPrice(event.priceCents)}</span>
                  <Link
                    to={`/events/${event.id}`}
                    aria-label={`View ${event.title}`}
                    className="block mt-2 text-sm bg-cyan-600 text-white rounded-lg px-4 py-2 hover:bg-cyan-700 transition-colors no-underline"
                  >
                    View →
                  </Link>
                </div>
              </div>
            </div>
          </li>
        ))}
      </ul>
      {events.length === 0 && (
        <p className="text-center text-gray-400 py-12">No events match your search.</p>
      )}
      <div ref={sentinelRef} data-testid="events-sentinel" className="h-1" />
      {loadingMore && <p className="text-gray-500 text-center py-6">Loading more events...</p>}
    </div>
  )
}
