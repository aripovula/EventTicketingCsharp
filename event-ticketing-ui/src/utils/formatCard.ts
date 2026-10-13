import type { SavedCard } from '../api/paymentMethod'

// Stripe's lowercase brand ids, as people write them.
const BRANDS: Record<string, string> = {
  amex: 'American Express',
  diners: 'Diners Club',
  discover: 'Discover',
  eftpos_au: 'eftpos',
  jcb: 'JCB',
  mastercard: 'Mastercard',
  unionpay: 'UnionPay',
  visa: 'Visa',
}

/** "Visa ending in 4242" */
export function formatCard(card: SavedCard): string {
  return `${BRANDS[card.brand] ?? 'Card'} ending in ${card.last4}`
}

/** "12/2029" */
export function formatCardExpiry(card: SavedCard): string {
  return `${String(card.expMonth).padStart(2, '0')}/${card.expYear}`
}
