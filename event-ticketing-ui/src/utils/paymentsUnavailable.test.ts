import { paymentsUnavailableMessage } from './paymentsUnavailable'

describe('paymentsUnavailableMessage', () => {
  it('says how long to wait', () => {
    const res = new Response(null, { status: 503, headers: { 'Retry-After': '30' } })

    expect(paymentsUnavailableMessage(res)).toBe('Payments are temporarily unavailable. Please try again in 30 seconds.')
  })

  it('asks for a minute when the wait is unknown', () => {
    expect(paymentsUnavailableMessage(new Response(null, { status: 503 })))
      .toBe('Payments are temporarily unavailable. Please try again in a minute.')
  })
})
