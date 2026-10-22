import { useEffect, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { fetchOrder, type OrderItem } from '../api/orders'
import { formatPrice } from '../utils/formatPrice'
import TechNote from '../components/TechNote'

export default function OrderConfirmationPage() {
  const { id } = useParams<{ id: string }>()
  const [searchParams] = useSearchParams()
  const guestToken = searchParams.get('token')
  const [order, setOrder] = useState<OrderItem | null>(null)
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)

  useEffect(() => {
    fetchOrder(Number(id), guestToken)
      .then(res => {
        if (res.status === 404) { setNotFound(true); return null }
        return res.ok ? res.json() : null
      })
      .then(body => setOrder(body?.data ?? null))
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [id, guestToken])

  if (loading) return <p className="text-gray-500 text-center py-12">Loading…</p>
  if (notFound) return <p className="text-center text-gray-500 py-12">Order not found.</p>
  if (!order) return <p className="text-center text-red-500 py-12">Something went wrong. Please try again.</p>

  return (
    <div className="max-w-lg mx-auto">
      <TechNote points={[
        <><strong>transactional outbox</strong>: the order and its confirmation message are saved in one transaction, then a worker publishes it to <strong>RabbitMQ</strong>, so a crash can&apos;t lose it</>,
        <><strong>guest links</strong> carry a random token stored only as a SHA-256 hash, so a leaked database can&apos;t open orders</>,
      ]} />
      <div className="bg-white rounded-xl border border-gray-200 p-8 text-center">
        <p className="text-4xl mb-4" aria-hidden="true">🎟</p>
        <h1 className="text-2xl font-bold text-gray-900 mb-1">You're going!</h1>
        <p className="text-gray-500 text-sm mb-6">
          Confirmation sent to <span className="font-medium text-gray-700">{order.email}</span>
        </p>
        <div className="text-left border-t border-gray-100 pt-5 space-y-2">
          <p className="text-lg font-semibold text-gray-900">{order.eventTitle}</p>
          <p className="text-sm text-gray-500">
            {order.eventVenue} · {new Date(order.eventStartTime).toLocaleDateString(undefined, { dateStyle: 'long' })}
          </p>
          <p className="text-sm text-gray-500">
            Order #{order.id} · <span className="font-medium text-gray-700">{formatPrice(order.priceCents)}</span>
          </p>
        </div>
      </div>
      <div className="mt-6 text-center">
        <Link to="/" className="text-sm text-cyan-600 hover:text-cyan-800 transition-colors no-underline">
          ← Browse more events
        </Link>
      </div>
    </div>
  )
}
