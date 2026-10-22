import { useEffect, useRef, useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import FullCalendar from '@fullcalendar/react'
import dayGridPlugin from '@fullcalendar/daygrid'
import type { EventClickArg, EventHoveringArg } from '@fullcalendar/core'
import { fetchAllMyOrders, type OrderItem } from '../api/orders'
import { useAuth } from '../context/useAuth'
import { colorForType } from '../utils/eventTypes'
import { formatPrice } from '../utils/formatPrice'
import TechNote from '../components/TechNote'

const time24 = (iso: string) => new Date(iso).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit', hour12: false })

export default function MyCalendarPage() {
  const { user, loading: authLoading } = useAuth()
  const [orders, setOrders] = useState<OrderItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [tooltip, setTooltip] = useState<{ order: OrderItem; top: number; left: number } | null>(null)
  const [popup, setPopup] = useState<{ order: OrderItem; top: number; left: number } | null>(null)
  const popupRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!user) return
    fetchAllMyOrders()
      .then(setOrders)
      .catch(() => setError('Could not load your bookings.'))
  }, [user])

  // Clicking anywhere outside the popup closes it.
  useEffect(() => {
    function handleMouseDown(e: MouseEvent) {
      if (popupRef.current && !popupRef.current.contains(e.target as Node)) setPopup(null)
    }
    document.addEventListener('mousedown', handleMouseDown)
    return () => document.removeEventListener('mousedown', handleMouseDown)
  }, [])

  if (authLoading) return null
  if (!user) return <Navigate to="/login" replace />

  const calendarEvents = orders.map(order => ({
    id: String(order.id),
    title: order.eventTitle,
    start: order.eventStartTime,
    end: order.eventEndTime,
    backgroundColor: colorForType(order.eventType),
    borderColor: colorForType(order.eventType),
    extendedProps: { order },
  }))

  function handleEventMouseEnter(arg: EventHoveringArg) {
    const rect = arg.el.getBoundingClientRect()
    setTooltip({ order: arg.event.extendedProps.order as OrderItem, top: rect.top - 8, left: rect.left + rect.width / 2 })
  }

  function handleEventClick(arg: EventClickArg) {
    const rect = arg.el.getBoundingClientRect()
    // Page coordinates, so a popup under a low calendar row can still be scrolled into view.
    setPopup({ order: arg.event.extendedProps.order as OrderItem, top: rect.bottom + window.scrollY + 6, left: rect.left + window.scrollX })
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">My Calendar</h1>
      <TechNote points={[
        <><strong>FullCalendar</strong> shows the events you have tickets for, colored by type, in your own time zone (the API stores and sends UTC).</>,
        <>The page follows the same cursor-paginated orders API as My orders until it has every page. Click an event to open its order.</>,
      ]} />
      {error && <p className="text-center text-red-500 py-4">{error}</p>}
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
          <span className="font-medium">{tooltip.order.eventTitle}</span>
          <span className="text-gray-300 ml-2">
            {time24(tooltip.order.eventStartTime)} – {time24(tooltip.order.eventEndTime)}
          </span>
        </div>
      )}

      {popup && (
        <div
          ref={popupRef}
          role="dialog"
          aria-label={popup.order.eventTitle}
          className="absolute z-50 bg-white rounded-xl border border-gray-200 shadow-lg p-4 w-72"
          style={{ top: popup.top, left: popup.left }}
        >
          <div className="flex items-start justify-between gap-2 mb-2">
            <span
              className="text-xs font-medium text-white px-2 py-0.5 rounded-full"
              style={{ backgroundColor: colorForType(popup.order.eventType) }}
            >
              {popup.order.eventType}
            </span>
            <button onClick={() => setPopup(null)} aria-label="Close" className="text-gray-400 hover:text-gray-600 text-lg leading-none">×</button>
          </div>
          <h3 className="font-semibold text-gray-900 text-sm mb-1">{popup.order.eventTitle}</h3>
          <p className="text-xs text-gray-500 mb-1">{popup.order.eventVenue}</p>
          <p className="text-xs text-gray-500 mb-3">
            {new Date(popup.order.eventStartTime).toLocaleDateString(undefined, { dateStyle: 'medium' })}{' '}
            {time24(popup.order.eventStartTime)}{' – '}{time24(popup.order.eventEndTime)}
          </p>
          <div className="flex items-center justify-between text-xs text-gray-500">
            <span>Order #{popup.order.id}</span>
            <span className="font-medium text-gray-900">{formatPrice(popup.order.priceCents)}</span>
          </div>
          <Link to={`/orders/${popup.order.id}`} className="block mt-3 text-xs text-cyan-600 hover:underline">
            View order
          </Link>
        </div>
      )}
    </div>
  )
}
