import { useState, type ReactNode } from 'react'
import { CardElement, Elements, useElements, useStripe } from '@stripe/react-stripe-js'
import { stripePromise, TEST_CARDS } from './stripe'

/** Turns the buyer's card into a Stripe PaymentMethod id, or an error to show. */
export type CreatePaymentMethod = () => Promise<{ id: string } | { error: string }>

export type CardInputParts = {
  /** False until Stripe.js has loaded. */
  ready: boolean
  createPaymentMethod: CreatePaymentMethod
  field: ReactNode
  note: ReactNode
}

type Props = { children: (card: CardInputParts) => ReactNode }

const labelClass = 'block text-sm font-medium text-gray-700 mb-1'

// Matches the Stripe card field to the app's other inputs.
const CARD_ELEMENT_OPTIONS = {
  style: {
    base: { fontSize: '14px', color: '#111827', fontFamily: 'inherit', '::placeholder': { color: '#9ca3af' } },
    invalid: { color: '#ef4444' },
  },
}

/**
 * A card field for a form, passed to `children`: Stripe's card element when a publishable key is
 * set, otherwise a choice of Stripe's test cards.
 */
export default function CardInput({ children }: Props) {
  return stripePromise
    ? <Elements stripe={stripePromise}><StripeCardInput>{children}</StripeCardInput></Elements>
    : <TestCardInput>{children}</TestCardInput>
}

function StripeCardInput({ children }: Props) {
  const stripe = useStripe()
  const elements = useElements()

  async function createPaymentMethod() {
    const card = elements?.getElement(CardElement)
    if (!stripe || !card) return { error: 'Payments are still loading. Please try again.' }
    const { paymentMethod, error } = await stripe.createPaymentMethod({ type: 'card', card })
    return paymentMethod ? { id: paymentMethod.id } : { error: error?.message ?? 'Your card details could not be checked.' }
  }

  return children({
    ready: !!stripe,
    createPaymentMethod,
    field: (
      <div>
        <span className={labelClass}>Card details</span>
        <div className="rounded-lg border border-gray-300 px-3 py-2.5 focus-within:ring-2 focus-within:ring-cyan-400 transition">
          <CardElement options={CARD_ELEMENT_OPTIONS} />
        </div>
      </div>
    ),
    note: <>Your card details go straight to Stripe in a secure frame and never reach our server. Use test card <code>4242 4242 4242 4242</code>, any future expiry date and any CVC.</>,
  })
}

function TestCardInput({ children }: Props) {
  const [card, setCard] = useState(TEST_CARDS[0].id)

  return children({
    ready: true,
    createPaymentMethod: async () => ({ id: card }),
    field: (
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
    ),
    note: <>No Stripe key is set up here, so instead of a card form you pick one of Stripe&apos;s test cards.</>,
  })
}
