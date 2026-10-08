import { formatPrice } from './formatPrice'

describe('formatPrice', () => {
  it('formats cents as US dollars', () => {
    expect(formatPrice(2500)).toBe('$25.00')
    expect(formatPrice(14900)).toBe('$149.00')
    expect(formatPrice(1999)).toBe('$19.99')
  })

  it('formats zero as free of charge in dollars', () => {
    expect(formatPrice(0)).toBe('$0.00')
  })
})
