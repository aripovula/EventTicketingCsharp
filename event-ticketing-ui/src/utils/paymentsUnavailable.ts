/** What to tell the buyer when a payment endpoint answers 503: Stripe is failing and we've paused calling it. */
export function paymentsUnavailableMessage(res: Response): string {
  const wait = Number(res.headers.get('Retry-After'))
  return wait > 0
    ? `Payments are temporarily unavailable. Please try again in ${wait} seconds.`
    : 'Payments are temporarily unavailable. Please try again in a minute.'
}
