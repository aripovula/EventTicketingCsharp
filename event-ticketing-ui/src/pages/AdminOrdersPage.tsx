import { useCallback, useState } from 'react'
import { Link } from 'react-router-dom'
import { fetchAllOrders, type OrderItem } from '../api/orders'
import { useCursorPages } from '../hooks/useCursorPages'
import { formatPrice } from '../utils/formatPrice'

export default function AdminOrdersPage() {
  const fetchPage = useCallback((after: string | null) => fetchAllOrders(after), [])
  const { items: orders, loading, error, loadingMore, sentinelRef } = useCursorPages<OrderItem>(fetchPage)
  const [selectedEvent, setSelectedEvent] = useState('')

  // Like the old admin page, the filter works on the orders loaded so far.
  const eventTitles = [...new Set(orders.map(o => o.eventTitle))].sort()
  const filtered = selectedEvent ? orders.filter(o => o.eventTitle === selectedEvent) : orders

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900 m-0">All orders</h1>
        <Link to="/admin" className="text-sm text-cyan-600 hover:text-cyan-800 transition-colors no-underline">
          ← Back to admin
        </Link>
      </div>

      {loading && <p className="text-gray-500 text-center py-12">Loading...</p>}
      {error && <p className="text-red-500 text-center py-12">Failed to load orders.</p>}
      {!loading && !error && orders.length === 0 && <p className="text-gray-400 text-center py-12">No orders yet.</p>}

      {!loading && !error && orders.length > 0 && (
        <>
          <div className="mb-4">
            <select
              aria-label="Filter by event"
              value={selectedEvent}
              onChange={e => setSelectedEvent(e.target.value)}
              className="rounded-lg border border-gray-300 px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-cyan-400 transition"
            >
              <option value="">All events</option>
              {eventTitles.map(title => <option key={title} value={title}>{title}</option>)}
            </select>
          </div>
          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="text-left px-6 py-3 font-medium text-gray-500">Order #</th>
                  <th className="text-left px-6 py-3 font-medium text-gray-500">Event</th>
                  <th className="text-left px-6 py-3 font-medium text-gray-500">Email</th>
                  <th className="text-right px-6 py-3 font-medium text-gray-500">Price</th>
                  <th className="text-left px-6 py-3 font-medium text-gray-500">Booked at</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {filtered.map(order => (
                  <tr key={order.id} className="hover:bg-gray-50 transition-colors">
                    <td className="px-6 py-4 text-gray-500">#{order.id}</td>
                    <td className="px-6 py-4 font-medium text-gray-900">{order.eventTitle}</td>
                    <td className="px-6 py-4 text-gray-700">{order.email}</td>
                    <td className="px-6 py-4 text-right text-cyan-600 font-medium">{formatPrice(order.priceCents)}</td>
                    <td className="px-6 py-4 text-gray-500">
                      {new Date(order.bookedAt).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
      <div ref={sentinelRef} data-testid="admin-orders-sentinel" className="h-1" />
      {loadingMore && <p className="text-gray-500 text-center py-6">Loading more orders...</p>}
    </div>
  )
}
