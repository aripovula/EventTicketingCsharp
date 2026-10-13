import { useState, type ReactNode } from 'react'
import type { Payment } from '../api/bookings'
import type { SavedCard } from '../api/paymentMethod'
import CardInput, { type CreatePaymentMethod } from '../payments/CardInput'
import { formatCard, formatCardExpiry } from '../utils/formatCard'
import { formatPrice } from '../utils/formatPrice'

type Props = {
  eventTitle: string
  eventDate: string
  eventVenue: string
  priceCents: number
  savedEmail?: string
  /** The signed-in user's saved card, offered first when there is one. */
  savedCard?: SavedCard | null
  onConfirm: (email: string, payment: Payment) => Promise<void>
  onClose: () => void
  error: string | null
}

const labelClass = 'block text-sm font-medium text-gray-700 mb-1'

export default function BookingModal(props: Props) {
  if (props.priceCents === 0) {
    return (
      <BookingForm
        {...props}
        ready
        createPaymentMethod={null}
        paymentField={null}
        note="This event is free, so no card is needed."
      />
    )
  }
  return (
    <CardInput>
      {card => <BookingForm {...props} ready={card.ready} createPaymentMethod={card.createPaymentMethod} paymentField={card.field} note={card.note} />}
    </CardInput>
  )
}

type BookingFormProps = Props & {
  ready: boolean
  /** Null for a free event. */
  createPaymentMethod: CreatePaymentMethod | null
  paymentField: ReactNode
  note: ReactNode
}

function BookingForm({ eventTitle, eventDate, eventVenue, priceCents, savedEmail, savedCard, onConfirm, onClose, error, ready, createPaymentMethod, paymentField, note }: BookingFormProps) {
  const [email, setEmail] = useState(savedEmail ?? '')
  // Free events take no card, so there is nothing to choose between.
  const offerSavedCard = !!savedCard && !!createPaymentMethod
  const [useSavedCard, setUseSavedCard] = useState(offerSavedCard)
  const [submitting, setSubmitting] = useState(false)
  const [cardError, setCardError] = useState<string | null>(null)

  const formattedDate = new Date(eventDate).toLocaleDateString(undefined, { dateStyle: 'long' })
  const shownError = cardError ?? error

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setCardError(null)
    setSubmitting(true)
    try {
      if (!createPaymentMethod) {
        await onConfirm(email, null)
        return
      }
      if (useSavedCard) {
        await onConfirm(email, { useSavedPaymentMethod: true })
        return
      }
      const card = await createPaymentMethod()
      if ('error' in card) {
        setCardError(card.error)
        return
      }
      await onConfirm(email, { paymentMethodId: card.id })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div role="dialog" aria-modal="true" aria-labelledby="booking-modal-title" className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-xl border border-gray-200 p-6 w-full max-w-md mx-4 shadow-lg">
        <div className="flex items-start justify-between mb-1">
          <h2 id="booking-modal-title" className="text-xl font-bold text-gray-900">Place order</h2>
          <span className="text-xs font-medium bg-amber-100 text-amber-700 px-2 py-0.5 rounded-full">Test mode</span>
        </div>
        <p className="text-sm text-gray-500 mb-4">
          {eventTitle} · {eventVenue} · {formattedDate} &mdash; <span className="font-medium text-gray-700">{formatPrice(priceCents)}</span>
        </p>

        <form onSubmit={handleSubmit} aria-label="booking form" className="space-y-4">
          <div>
            <label htmlFor="booking-email" className={labelClass}>Email</label>
            <input
              id="booking-email"
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
              placeholder="you@example.com"
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-cyan-400 transition"
            />
          </div>

          {offerSavedCard && (
            <fieldset>
              <legend className={labelClass}>Pay with</legend>
              <div className="space-y-1">
                <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
                  <input type="radio" name="card-choice" checked={useSavedCard} onChange={() => setUseSavedCard(true)} />
                  {formatCard(savedCard!)} (expires {formatCardExpiry(savedCard!)})
                </label>
                <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
                  <input type="radio" name="card-choice" checked={!useSavedCard} onChange={() => setUseSavedCard(false)} />
                  A different card
                </label>
              </div>
            </fieldset>
          )}

          {!useSavedCard && paymentField}

          {shownError && <p role="alert" className="text-sm text-red-500">{shownError}</p>}

          <div className="flex gap-3 pt-2">
            <button
              type="submit"
              disabled={submitting || !(ready || useSavedCard)}
              className="bg-cyan-600 text-white px-5 py-2 rounded-lg text-sm font-medium hover:bg-cyan-700 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
            >
              {submitting ? 'Placing order…' : 'Place order'}
            </button>
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="border border-gray-300 text-gray-600 px-5 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 transition-colors disabled:opacity-40"
            >
              Cancel
            </button>
          </div>

          {!useSavedCard && <p className="text-xs text-gray-500 leading-relaxed">{note}</p>}
        </form>
      </div>
    </div>
  )
}
