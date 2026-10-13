import { formatCard, formatCardExpiry } from './formatCard'

const card = { brand: 'visa', last4: '4242', expMonth: 3, expYear: 2029 }

describe('formatCard', () => {
  it('names the brand and the last four digits', () => {
    expect(formatCard(card)).toBe('Visa ending in 4242')
    expect(formatCard({ ...card, brand: 'amex', last4: '8431' })).toBe('American Express ending in 8431')
  })

  it('falls back to "Card" for a brand it does not know', () => {
    expect(formatCard({ ...card, brand: 'unknown' })).toBe('Card ending in 4242')
  })
})

describe('formatCardExpiry', () => {
  it('pads the month', () => {
    expect(formatCardExpiry(card)).toBe('03/2029')
  })
})
