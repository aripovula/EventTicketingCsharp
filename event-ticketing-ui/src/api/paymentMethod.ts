/** The signed-in user's saved card; Stripe keeps the card itself. */
export interface SavedCard {
  brand: string
  last4: string
  expMonth: number
  expYear: number
}

export function fetchMyPaymentMethod() {
  return fetch('/api/v1/auth/me/payment-method', { credentials: 'include' })
}
