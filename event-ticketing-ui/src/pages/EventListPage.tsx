import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { fetchEvents, type EventItem } from '../api/events'
import { colorForType } from '../utils/eventTypes'
import { formatPrice } from '../utils/formatPrice'

const time = (iso: string) => new Date(iso).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })

export default function EventListPage() {
  const [events, setEvents] = useState<EventItem[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetchEvents()
      .then(res => res.json())
      .then(body => setEvents(body.data ?? []))
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [])

  if (loading) return <p className="text-gray-500 text-center py-12">Loading events...</p>

  return (
    <div>
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
    </div>
  )
}
