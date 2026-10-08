import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { fetchEvent, type EventItem } from '../api/events'
import { formatPrice } from '../utils/formatPrice'

const time = (iso: string) => new Date(iso).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })

export default function EventDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [event, setEvent] = useState<EventItem | null>(null)
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)

  useEffect(() => {
    fetchEvent(Number(id))
      .then(res => {
        if (res.status === 404) { setNotFound(true); return null }
        return res.ok ? res.json() : null
      })
      .then(body => setEvent(body?.data ?? null))
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [id])

  useEffect(() => {
    document.title = event ? `${event.title} | Ticketing` : 'Ticketing'
    return () => { document.title = 'Event Ticketing' }
  }, [event])

  if (loading) return <p className="text-gray-500 text-center py-12">Loading...</p>
  if (notFound) return <p className="text-center text-gray-500 py-12">Event not found.</p>
  if (!event) return <p className="text-center text-red-500 py-12">Something went wrong. Please try again.</p>

  const soldOut = event.availableSeats === 0

  return (
    <div>
      <Link to="/" className="text-sm text-cyan-600 hover:text-cyan-800 transition-colors no-underline">
        ← Back to events
      </Link>

      <div className="mt-6 bg-white rounded-xl border border-gray-200 overflow-hidden">
        {event.imageUrl && <img src={event.imageUrl} alt={event.title} className="w-full h-64 object-cover" />}
        <div className="p-6">
          <h1 className="text-3xl font-bold text-gray-900 mt-0 mb-2">{event.title}</h1>
          <p className="text-gray-500 text-sm mb-6">
            {event.venue} · {new Date(event.startTime).toLocaleDateString(undefined, { dateStyle: 'long' })}{' '}
            {time(event.startTime)}{' – '}{time(event.endTime)}
          </p>
          <p className="text-gray-700 leading-relaxed mb-6">{event.description}</p>
          <div className="border-t border-gray-100 pt-5">
            <span className="text-3xl font-bold text-cyan-600">{formatPrice(event.priceCents)}</span>
            <p className="text-sm mt-1 text-gray-500">
              {soldOut
                ? <span className="text-red-500 font-medium">Sold out</span>
                : <>{event.availableSeats} of {event.totalSeats} seats available</>}
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
