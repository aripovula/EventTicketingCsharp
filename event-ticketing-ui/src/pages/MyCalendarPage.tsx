import { useEffect, useState } from 'react'
import { Navigate } from 'react-router-dom'
import FullCalendar from '@fullcalendar/react'
import dayGridPlugin from '@fullcalendar/daygrid'
import { fetchAllMyOrders, type OrderItem } from '../api/orders'
import { useAuth } from '../context/useAuth'
import { colorForType } from '../utils/eventTypes'

export default function MyCalendarPage() {
  const { user, loading: authLoading } = useAuth()
  const [orders, setOrders] = useState<OrderItem[]>([])
  const [error, setError] = useState<string | null>(null)

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

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">My Calendar</h1>
      {error && <p className="text-center text-red-500 py-4">{error}</p>}
      <div className="bg-white rounded-xl border border-gray-200 p-4">
        <FullCalendar
          plugins={[dayGridPlugin]}
          initialView="dayGridMonth"
          events={calendarEvents}
          height="auto"
          headerToolbar={{ left: 'prev,next today', center: 'title', right: '' }}
          eventDisplay="block"
          dayMaxEvents={false}
        />
      </div>
    </div>
  )
}
