import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import FullCalendar from '@fullcalendar/react'
import dayGridPlugin from '@fullcalendar/daygrid'
import type { EventClickArg, EventHoveringArg } from '@fullcalendar/core'
import { fetchAllEvents, type EventItem } from '../api/events'
import { EVENT_TYPE_COLORS, EVENT_TYPES, colorForType } from '../utils/eventTypes'
import { formatPrice } from '../utils/formatPrice'
import TechNote from '../components/TechNote'

const time = (iso: string) => new Date(iso).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })

export default function AdminCalendarPage() {
  const [events, setEvents] = useState<EventItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [tooltip, setTooltip] = useState<{ title: string; availableSeats: number; top: number; left: number } | null>(null)
  const [popup, setPopup] = useState<{ event: EventItem; top: number; left: number } | null>(null)
  const popupRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    fetchAllEvents()
      .then(setEvents)
      .catch((err: Error) => setError(err.message.startsWith('Failed') ? err.message : 'Could not load events.'))
      .finally(() => setLoading(false))
  }, [])

  // Clicking anywhere outside the popup closes it.
  useEffect(() => {
    function handleMouseDown(e: MouseEvent) {
      if (popupRef.current && !popupRef.current.contains(e.target as Node)) setPopup(null)
    }
    document.addEventListener('mousedown', handleMouseDown)
    return () => document.removeEventListener('mousedown', handleMouseDown)
  }, [])

  const calendarEvents = events.map(ev => ({
    id: String(ev.id),
    title: `${ev.title} – ${ev.availableSeats}`,
    start: ev.startTime,
    end: ev.endTime,
    backgroundColor: colorForType(ev.eventType),
    borderColor: colorForType(ev.eventType),
    extendedProps: { event: ev },
  }))

  function handleEventMouseEnter(arg: EventHoveringArg) {
    const ev = arg.event.extendedProps.event as EventItem
    const rect = arg.el.getBoundingClientRect()
    setTooltip({ title: ev.title, availableSeats: ev.availableSeats, top: rect.top - 8, left: rect.left + rect.width / 2 })
  }

  function handleEventClick(arg: EventClickArg) {
    const rect = arg.el.getBoundingClientRect()
    // Page coordinates, so a popup under a low calendar row can still be scrolled into view.
    setPopup({ event: arg.event.extendedProps.event as EventItem, top: rect.bottom + window.scrollY + 6, left: rect.left + window.scrollX })
  }

  if (loading) return <p className="text-gray-500 text-center py-12">Loading...</p>
  if (error) return <p className="text-center text-red-500 py-12">{error}</p>

  return (
    <div>
      <div className="mb-6">
        <Link to="/admin" className="text-sm text-cyan-600 hover:text-cyan-800 transition-colors no-underline">
          ← Back to admin
        </Link>
        <h1 className="text-2xl font-bold text-gray-900 m-0">Events Calendar</h1>
      </div>

      <TechNote points={[
        <><strong>FullCalendar</strong> shows every event colored by type, with the seats still left, in your own time zone (the API stores and sends UTC).</>,
        <>The page follows the cursor-paginated events API until it has every page, so the calendar is complete however many events there are.</>,
      ]} />

      <ul aria-label="event type legend" className="flex flex-wrap gap-3 mb-5 list-none p-0 m-0">
        {EVENT_TYPES.filter(t => t !== 'Other').map(type => (
          <li key={type} className="flex items-center gap-1.5 text-xs font-medium text-gray-600">
            <span className="w-3 h-3 rounded-full inline-block" style={{ backgroundColor: EVENT_TYPE_COLORS[type] }} />
            {type}
          </li>
        ))}
      </ul>
      <p className="text-xs text-gray-400 mb-4">
        Each event shows: <em>Title – remaining seats</em>. Overlapping events share the same time slot.
      </p>

      <div className="bg-white rounded-xl border border-gray-200 p-4">
        <FullCalendar
          plugins={[dayGridPlugin]}
          initialView="dayGridMonth"
          events={calendarEvents}
          eventMouseEnter={handleEventMouseEnter}
          eventMouseLeave={() => setTooltip(null)}
          eventClick={handleEventClick}
          height="auto"
          headerToolbar={{ left: 'prev,next today', center: 'title', right: '' }}
          eventDisplay="block"
          dayMaxEvents={false}
        />
      </div>

      {tooltip && (
        <div
          role="tooltip"
          className="fixed z-50 pointer-events-none -translate-x-1/2 -translate-y-full bg-gray-900 text-white text-xs rounded-lg px-3 py-2 shadow-lg whitespace-nowrap"
          style={{ top: tooltip.top, left: tooltip.left }}
        >
          <span className="font-medium">{tooltip.title}</span>
          <span className="text-gray-300 ml-2">{tooltip.availableSeats} seats left</span>
        </div>
      )}

      {popup && (
        <div
          ref={popupRef}
          role="dialog"
          aria-label={popup.event.title}
          className="absolute z-50 bg-white rounded-xl border border-gray-200 shadow-lg p-4 w-80"
          style={{ top: popup.top, left: popup.left }}
        >
          <div className="flex items-start justify-between gap-2 mb-2">
            <span
              className="text-xs font-medium text-white px-2 py-0.5 rounded-full"
              style={{ backgroundColor: colorForType(popup.event.eventType) }}
            >
              {popup.event.eventType}
            </span>
            <button onClick={() => setPopup(null)} aria-label="Close" className="text-gray-400 hover:text-gray-600 text-lg leading-none">×</button>
          </div>
          <h3 className="font-semibold text-gray-900 text-sm mb-1">{popup.event.title}</h3>
          <p className="text-xs text-gray-500 mb-1">{popup.event.venue}</p>
          <p className="text-xs text-gray-500 mb-1">
            {new Date(popup.event.startTime).toLocaleDateString(undefined, { dateStyle: 'medium' })}{' '}
            {time(popup.event.startTime)}{' – '}{time(popup.event.endTime)}
          </p>
          <p className="text-xs text-gray-600 line-clamp-3 mb-3">{popup.event.description}</p>
          <div className="flex items-center justify-between text-xs">
            <span className="text-gray-500">{popup.event.availableSeats} / {popup.event.totalSeats} seats remaining</span>
            <span className="font-medium text-gray-900">{formatPrice(popup.event.priceCents)}</span>
          </div>
          <div className="flex gap-2 mt-3">
            <Link
              to={`/admin/${popup.event.id}/edit`}
              className="text-xs border border-gray-300 text-gray-600 px-3 py-1 rounded-lg hover:bg-gray-50 transition-colors no-underline"
            >
              Edit
            </Link>
          </div>
        </div>
      )}
    </div>
  )
}
