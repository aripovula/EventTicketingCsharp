import { dollarsToCents, localInputToIso, toEventInput, type EventFormData } from './eventForm'

const form: EventFormData = {
  title: 'Brand New Gig',
  venue: 'Main Hall',
  description: 'A new gig.',
  imageUrl: '',
  startTime: '2026-11-02T19:00',
  endTime: '2026-11-02T22:00',
  eventType: 'Music',
  totalSeats: '100',
  availableSeats: '80',
  price: '19.99',
}

describe('eventForm', () => {
  it('converts a local datetime-local value to UTC ISO', () => {
    expect(localInputToIso('2026-11-02T19:00')).toBe(new Date(2026, 10, 2, 19, 0).toISOString())
  })

  it('converts dollars to whole cents without float drift', () => {
    expect(dollarsToCents('19.99')).toBe(1999)
    expect(dollarsToCents('0.29')).toBe(29)
    expect(dollarsToCents('25')).toBe(2500)
  })

  it('builds the API payload from the form', () => {
    expect(toEventInput(form)).toEqual({
      title: 'Brand New Gig',
      venue: 'Main Hall',
      description: 'A new gig.',
      imageUrl: null,
      startTime: new Date(2026, 10, 2, 19, 0).toISOString(),
      endTime: new Date(2026, 10, 2, 22, 0).toISOString(),
      eventType: 'Music',
      totalSeats: 100,
      availableSeats: 80,
      priceCents: 1999,
    })
  })

  it('keeps a given image URL', () => {
    expect(toEventInput({ ...form, imageUrl: ' https://example.com/a.jpg ' }).imageUrl).toBe('https://example.com/a.jpg')
  })
})
