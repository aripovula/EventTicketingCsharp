import { useCallback } from 'react'
import { Link } from 'react-router-dom'
import { fetchMyOrders, type OrderItem } from '../api/orders'
import { useAuth } from '../context/useAuth'
import { useCursorPages } from '../hooks/useCursorPages'
import { formatPrice } from '../utils/formatPrice'
import TechNote from '../components/TechNote'

export default function MyOrdersPage() {
  const { user, loading: authLoading } = useAuth()
  const fetchPage = useCallback((after: string | null) => fetchMyOrders(after), [])
  const { items: orders, loading, error, loadingMore, sentinelRef } = useCursorPages<OrderItem>(fetchPage, !!user)

  if (authLoading) return <p className="text-gray-500 text-center py-12">Loading...</p>

  return (
    <div className="max-w-lg mx-auto">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">My orders</h1>
      <TechNote points={[
        <><strong>cursor pagination</strong> loads 20 orders at a time, newest first, and stays fast however many there are</>,
        <><strong>only yours</strong>: the API reads who you are from the signed HttpOnly JWT cookie, not from a user id anyone could change, and scripts can&apos;t steal it</>,
      ]} />
      {!user && (
        <p className="text-sm text-gray-600">
          <Link to="/login" className="text-cyan-600 hover:underline">Sign in</Link> to see your orders.
          Booked as a guest? Use the link on your order confirmation.
        </p>
      )}

      {user && loading && <p className="text-gray-500 text-center py-8">Loading...</p>}
      {user && error && <p className="text-center text-red-500 text-sm">Something went wrong. Please try again.</p>}
      {user && !loading && !error && orders.length === 0 && (
        <p className="text-center text-gray-500 text-sm">No orders found.</p>
      )}

      {user && orders.length > 0 && (
        <ul aria-label="orders" className="space-y-4 list-none p-0 m-0">
          {orders.map(order => (
            <li key={order.id} className="bg-white rounded-xl border border-gray-200 p-5">
              <p className="font-semibold text-gray-900 mb-1">{order.eventTitle}</p>
              <p className="text-sm text-gray-500 mb-2">
                {order.eventVenue} · {new Date(order.eventStartTime).toLocaleDateString(undefined, { dateStyle: 'long' })}
              </p>
              <p className="text-sm text-gray-500">
                Order #{order.id} · <span className="font-medium text-gray-700">{formatPrice(order.priceCents)}</span>
              </p>
              <Link
                to={`/orders/${order.id}`}
                className="text-xs text-cyan-600 hover:text-cyan-800 transition-colors no-underline mt-2 inline-block"
              >
                View confirmation →
              </Link>
            </li>
          ))}
        </ul>
      )}
      {user && <div ref={sentinelRef} data-testid="orders-sentinel" className="h-1" />}
      {loadingMore && <p className="text-gray-500 text-center py-6">Loading more orders...</p>}
    </div>
  )
}
