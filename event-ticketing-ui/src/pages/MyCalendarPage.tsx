import { useEffect, useState } from 'react'
import { Navigate } from 'react-router-dom'
import FullCalendar from '@fullcalendar/react'
import dayGridPlugin from '@fullcalendar/daygrid'
import type { EventHoveringArg } from '@fullcalendar/core'
import { fetchAllMyOrders, type OrderItem } from '../api/orders'
import { useAuth } from '../context/useAuth'
import { colorForType } from '../utils/eventTypes'

const time24 = (iso: string) => new Date(iso).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit', hour12: false })

export default function MyCalendarPage() {
  const { user, loading: authLoading } = useAuth()
  const [orders, setOrders] = useState<OrderItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [tooltip, setTooltip] = useState<{ order: OrderItem; top: number; left: number } | null>(null)

  useEffect(() => {
    if (!user) return
    fetchAllMyOrders()
      .then(setOrders)
      .catch(() => setError('Could not load your bookings.'))
  }, [user])

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

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">My Calendar</h1>
      {error && <p className="text-center text-red-500 py-4">{error}</p>}
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
          <span className="font-medium">{tooltip.order.eventTitle}</span>
          <span className="text-gray-300 ml-2">
            {time24(tooltip.order.eventStartTime)} – {time24(tooltip.order.eventEndTime)}
          </span>
        </div>
      )}
    </div>
  )
}
