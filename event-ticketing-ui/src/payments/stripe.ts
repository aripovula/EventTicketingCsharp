// The pure entry point fetches Stripe.js only when loadStripe runs, so test-card mode never loads it.
import { loadStripe } from '@stripe/stripe-js/pure'

const publishableKey: string | undefined = import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY

/**
 * Stripe.js, loaded once so the script is fetched early and shared. Null without a publishable key
 * (CI and E2E runs): the booking form then offers Stripe's test cards instead of a card field.
 */
export const stripePromise = publishableKey ? loadStripe(publishableKey) : null

/** Stripe's test PaymentMethod ids; Stripe test mode and the API's fake gateway both accept them. */
export const TEST_CARDS = [
  { id: 'pm_card_visa', label: 'Visa 4242 4242 4242 4242 (approved)' },
  { id: 'pm_card_chargeDeclined', label: 'Visa 4000 0000 0000 0002 (declined)' },
]
