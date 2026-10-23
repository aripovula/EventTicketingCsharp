import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { bookEvent, type Payment } from '../api/bookings'
import { fetchEvent, type EventItem } from '../api/events'
import { fetchMyPaymentMethod, type SavedCard } from '../api/paymentMethod'
import BookingModal from '../components/BookingModal'
import { useAuth } from '../context/useAuth'
import { useHubEvents } from '../hooks/useHubEvents'
import { formatPrice } from '../utils/formatPrice'
import { paymentsUnavailableMessage } from '../utils/paymentsUnavailable'
import EventImage from '../components/EventImage'
import TechNote from '../components/TechNote'

const time = (iso: string) => new Date(iso).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })

export default function EventDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [event, setEvent] = useState<EventItem | null>(null)
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)
  const [modalOpen, setModalOpen] = useState(false)
  // One key per booking attempt: retrying from the same modal can't book twice; reopening it starts afresh.
  const [idempotencyKey, setIdempotencyKey] = useState('')
  const [bookingError, setBookingError] = useState<string | null>(null)
  const [savedCard, setSavedCard] = useState<SavedCard | null>(null)
  const navigate = useNavigate()
  const { user } = useAuth()

  const loadEvent = useCallback(() => {
    fetchEvent(Number(id))
      .then(res => {
        if (res.status === 404) { setNotFound(true); return null }
        return res.ok ? res.json() : null
      })
      .then(body => setEvent(body?.data ?? null))
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [id])

  useEffect(() => { loadEvent() }, [loadEvent])

  // A signed-in user's saved card is offered in the booking modal; guests and admins have none.
  useEffect(() => {
    if (!user || user.role === 'admin') return
    fetchMyPaymentMethod()
      .then(res => (res.ok ? res.json() : null))
      .then(body => setSavedCard(body?.data ?? null))
      .catch(() => {})
  }, [user])

  // Live updates: seats and details change under the visitor's feet; a deleted event sends them home,
  // while an archived one stays (order links point here) and stops selling.
  useHubEvents({
    BookingMade: eventId => { if (eventId === Number(id)) loadEvent() },
    EventUpdated: eventId => { if (eventId === Number(id)) loadEvent() },
    EventArchived: eventId => { if (eventId === Number(id)) loadEvent() },
    EventDeleted: eventId => { if (eventId === Number(id)) navigate('/') },
  })

  useEffect(() => {
    document.title = event ? `${event.title} | Ticketing` : 'Ticketing'
    return () => { document.title = 'Event Ticketing' }
  }, [event])

  if (loading) return <p className="text-gray-500 text-center py-12">Loading...</p>
  if (notFound) return <p className="text-center text-gray-500 py-12">Event not found.</p>
  if (!event) return <p className="text-center text-red-500 py-12">Something went wrong. Please try again.</p>

  const soldOut = event.availableSeats === 0

  async function handleConfirmBooking(email: string, payment: Payment) {
    setBookingError(null)
    const res = await bookEvent(event!.id, email, payment, idempotencyKey).catch(() => null)
    if (res?.ok) {
      const order = (await res.json()).data
      // Guests can only reach their order through the token, so it rides along in the link.
      navigate(order.guestAccessToken
        ? `/orders/${order.id}?token=${encodeURIComponent(order.guestAccessToken)}`
        : `/orders/${order.id}`)
    } else if (res?.status === 402) {
      const body = await res.json().catch(() => null)
      setBookingError(body?.errors?.[0]?.detail ?? 'Your card was declined.')
      // The API remembers the decline under this key, so trying another card needs a new one.
      setIdempotencyKey(crypto.randomUUID())
    } else if (res?.status === 409) {
      setBookingError('Sorry, this event just sold out.')
    } else if (res?.status === 429) {
      const wait = Number(res.headers.get('Retry-After'))
      setBookingError(wait > 0
        ? `Too many booking attempts. Please try again in ${wait} seconds.`
        : 'Too many booking attempts. Please wait a minute and try again.')
    } else if (res?.status === 503) {
      setBookingError(paymentsUnavailableMessage(res))
    } else {
      setBookingError('Booking failed. Please try again.')
    }
  }

  return (
    <div>
      <Link to="/" className="text-sm text-cyan-600 hover:text-cyan-800 transition-colors no-underline">
        ← Back to events
      </Link>
      <TechNote className="mt-6" points={[
        <>orders go to <strong>a real Stripe test account</strong> as real test PaymentIntents (card 4242 4242 4242 4242, any future date and CVC; 4000 0000 0000 0002 is declined)</>,
        <><strong>Stripe Elements</strong> keeps the card number off our server, out of PCI scope</>,
        <><strong>no overselling</strong> (an atomic conditional update): one SQL <code>UPDATE … WHERE available_seats &gt; 0</code> reserves the seat before the charge, so two buyers can never both get the last one</>,
        <><strong>no double charges</strong>: an <code>Idempotency-Key</code>, also passed to Stripe, makes retries safe</>,
        <><strong>SignalR</strong> keeps the seat count live</>,
      ]} />

      <div className="mt-6 bg-white rounded-xl border border-gray-200 overflow-hidden">
        {event.imageUrl && <EventImage src={event.imageUrl} alt={event.title} className="w-full h-64 object-cover" />}
        <div className="p-6">
          <h1 className="text-3xl font-bold text-gray-900 mt-0 mb-2">{event.title}</h1>
          <p className="text-gray-500 text-sm mb-6">
            {event.venue} · {new Date(event.startTime).toLocaleDateString(undefined, { dateStyle: 'long' })}{' '}
            {time(event.startTime)}{' – '}{time(event.endTime)}
          </p>
          <p className="text-gray-700 leading-relaxed mb-6">{event.description}</p>
          <div className="flex items-center justify-between border-t border-gray-100 pt-5">
            <div>
              <span className="text-3xl font-bold text-cyan-600">{formatPrice(event.priceCents)}</span>
              <p className="text-sm mt-1 text-gray-500">
                {soldOut
                  ? <span className="text-red-500 font-medium">Sold out</span>
                  : <>{event.availableSeats} of {event.totalSeats} seats available</>}
              </p>
            </div>
            {event.archivedAt ? (
              <p className="text-sm text-gray-500 text-right max-w-xs m-0">
                This event has been archived and no longer sells tickets.
              </p>
            ) : user?.role === 'admin' ? (
              // The API refuses admin bookings too; this just says so up front.
              <p className="text-sm text-gray-500 text-right max-w-xs m-0">
                Admins can't book tickets. Sign in with a customer account to buy one.
              </p>
            ) : (
              <button
                onClick={() => { setIdempotencyKey(crypto.randomUUID()); setModalOpen(true) }}
                disabled={soldOut}
                className="bg-cyan-600 text-white px-6 py-2.5 rounded-lg font-medium hover:bg-cyan-700 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
              >
                {soldOut ? 'Sold out' : 'Buy ticket'}
              </button>
            )}
          </div>
        </div>
      </div>

      {modalOpen && (
        <BookingModal
          eventTitle={event.title}
          eventDate={event.startTime}
          eventVenue={event.venue}
          priceCents={event.priceCents}
          savedEmail={user?.email}
          savedCard={user ? savedCard : null}
          onConfirm={handleConfirmBooking}
          onClose={() => { setModalOpen(false); setBookingError(null) }}
          error={bookingError}
        />
      )}
    </div>
  )
}
