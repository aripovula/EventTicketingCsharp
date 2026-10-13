/** How a booking pays: a card from the form, the signed-in user's saved card, or nothing for a free event. */
export type Payment = { paymentMethodId: string } | { useSavedPaymentMethod: true } | null

/** The same idempotencyKey on a retry makes the API replay the first answer instead of booking twice. */
export function bookEvent(eventId: number, email: string, payment: Payment, idempotencyKey: string) {
  return fetch(`/api/v1/events/${eventId}/bookings`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Idempotency-Key': idempotencyKey },
    credentials: 'include',
    body: JSON.stringify({ email, ...payment }),
  })
}
