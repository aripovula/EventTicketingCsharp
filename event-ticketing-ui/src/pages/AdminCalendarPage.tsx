import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import FullCalendar from '@fullcalendar/react'
import dayGridPlugin from '@fullcalendar/daygrid'
import type { EventHoveringArg } from '@fullcalendar/core'
import { fetchAllEvents, type EventItem } from '../api/events'
import { EVENT_TYPE_COLORS, EVENT_TYPES, colorForType } from '../utils/eventTypes'

export default function AdminCalendarPage() {
  const [events, setEvents] = useState<EventItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [tooltip, setTooltip] = useState<{ title: string; availableSeats: number; top: number; left: number } | null>(null)

  useEffect(() => {
    fetchAllEvents()
      .then(setEvents)
      .catch((err: Error) => setError(err.message.startsWith('Failed') ? err.message : 'Could not load events.'))
      .finally(() => setLoading(false))
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
    </div>
  )
}
