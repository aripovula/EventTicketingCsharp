import { useState, type ReactNode } from 'react'
import { CardElement, Elements, useElements, useStripe } from '@stripe/react-stripe-js'
import { stripePromise, TEST_CARDS } from '../payments/stripe'
import { formatPrice } from '../utils/formatPrice'

type Props = {
  eventTitle: string
  eventDate: string
  eventVenue: string
  priceCents: number
  savedEmail?: string
  onConfirm: (email: string, paymentMethodId: string) => Promise<void>
  onClose: () => void
  error: string | null
}

/** Turns the buyer's card into a Stripe PaymentMethod id, or an error to show. */
type CreatePaymentMethod = () => Promise<{ id: string } | { error: string }>

const labelClass = 'block text-sm font-medium text-gray-700 mb-1'

// Matches the Stripe card field to the app's other inputs.
const CARD_ELEMENT_OPTIONS = {
  style: {
    base: { fontSize: '14px', color: '#111827', fontFamily: 'inherit', '::placeholder': { color: '#9ca3af' } },
    invalid: { color: '#ef4444' },
  },
}

export default function BookingModal(props: Props) {
  return stripePromise
    ? <Elements stripe={stripePromise}><StripeBookingForm {...props} /></Elements>
    : <TestCardBookingForm {...props} />
}

function StripeBookingForm(props: Props) {
  const stripe = useStripe()
  const elements = useElements()

  async function createPaymentMethod() {
    const card = elements?.getElement(CardElement)
    if (!stripe || !card) return { error: 'Payments are still loading. Please try again.' }
    const { paymentMethod, error } = await stripe.createPaymentMethod({ type: 'card', card })
    return paymentMethod ? { id: paymentMethod.id } : { error: error?.message ?? 'Your card details could not be checked.' }
  }

  return (
    <BookingForm
      {...props}
      ready={!!stripe}
      createPaymentMethod={createPaymentMethod}
      paymentField={
        <div>
          <span className={labelClass}>Card details</span>
          <div className="rounded-lg border border-gray-300 px-3 py-2.5 focus-within:ring-2 focus-within:ring-cyan-400 transition">
            <CardElement options={CARD_ELEMENT_OPTIONS} />
          </div>
        </div>
      }
      note={<>Your card details go straight to Stripe in a secure frame and never reach our server. Use test card <code>4242 4242 4242 4242</code>, any future expiry date and any CVC.</>}
    />
  )
}

function TestCardBookingForm(props: Props) {
  const [card, setCard] = useState(TEST_CARDS[0].id)

  return (
    <BookingForm
      {...props}
      ready
      createPaymentMethod={async () => ({ id: card })}
      paymentField={
        <fieldset>
          <legend className={labelClass}>Test card</legend>
          <div className="space-y-1">
            {TEST_CARDS.map(c => (
              <label key={c.id} className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
                <input type="radio" name="test-card" value={c.id} checked={card === c.id} onChange={() => setCard(c.id)} />
                {c.label}
              </label>
            ))}
          </div>
        </fieldset>
      }
      note={<>No Stripe key is set up here, so instead of a card form you pick one of Stripe&apos;s test cards.</>}
    />
  )
}

type BookingFormProps = Props & {
  ready: boolean
  createPaymentMethod: CreatePaymentMethod
  paymentField: ReactNode
  note: ReactNode
}

function BookingForm({ eventTitle, eventDate, eventVenue, priceCents, savedEmail, onConfirm, onClose, error, ready, createPaymentMethod, paymentField, note }: BookingFormProps) {
  const [email, setEmail] = useState(savedEmail ?? '')
  const [submitting, setSubmitting] = useState(false)
  const [cardError, setCardError] = useState<string | null>(null)

  const formattedDate = new Date(eventDate).toLocaleDateString(undefined, { dateStyle: 'long' })
  const shownError = cardError ?? error

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setCardError(null)
    setSubmitting(true)
    try {
      const card = await createPaymentMethod()
      if ('error' in card) {
        setCardError(card.error)
        return
      }
      await onConfirm(email, card.id)
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

          {paymentField}

          {shownError && <p role="alert" className="text-sm text-red-500">{shownError}</p>}

          <div className="flex gap-3 pt-2">
            <button
              type="submit"
              disabled={submitting || !ready}
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

          <p className="text-xs text-gray-500 leading-relaxed">{note}</p>
        </form>
      </div>
    </div>
  )
}
