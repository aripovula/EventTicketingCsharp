/** The same idempotencyKey on a retry makes the API replay the first answer instead of booking twice. */
export function bookEvent(eventId: number, email: string, idempotencyKey: string) {
  return fetch(`/api/v1/events/${eventId}/bookings`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Idempotency-Key': idempotencyKey },
    credentials: 'include',
    body: JSON.stringify({ email }),
  })
}
