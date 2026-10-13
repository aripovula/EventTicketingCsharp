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

/** Saves a Stripe PaymentMethod id as the card bookings can pay with, replacing any earlier one. */
export function saveMyPaymentMethod(paymentMethodId: string) {
  return fetch('/api/v1/auth/me/payment-method', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ paymentMethodId }),
  })
}
