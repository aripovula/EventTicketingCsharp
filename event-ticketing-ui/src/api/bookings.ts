export function bookEvent(eventId: number, email: string) {
  return fetch(`/api/v1/events/${eventId}/bookings`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ email }),
  })
}
