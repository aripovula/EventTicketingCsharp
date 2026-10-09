import { centsToDollars, dollarsToCents, eventToForm, isoToLocalInput, localInputToIso, toEventInput, type EventFormData } from './eventForm'
import { makeEvent } from '../test/fixtures'

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

  it('converts a UTC timestamp back to a local datetime-local value', () => {
    const local = new Date(2026, 10, 2, 19, 0)

    expect(isoToLocalInput(local.toISOString())).toBe('2026-11-02T19:00')
  })

  it('round-trips local times without shifting them by the time-zone offset', () => {
    expect(isoToLocalInput(localInputToIso('2026-11-02T19:00'))).toBe('2026-11-02T19:00')
  })

  it('formats cents as dollars with two decimals', () => {
    expect(centsToDollars(1999)).toBe('19.99')
    expect(centsToDollars(2500)).toBe('25.00')
  })

  it('fills the form from an event', () => {
    const event = makeEvent({ startTime: new Date(2026, 10, 2, 19, 0).toISOString(), endTime: new Date(2026, 10, 2, 22, 0).toISOString(), imageUrl: null })

    expect(eventToForm(event)).toEqual({
      title: 'Jazz Night',
      venue: 'Blue Note Club',
      description: 'Live jazz.',
      imageUrl: '',
      startTime: '2026-11-02T19:00',
      endTime: '2026-11-02T22:00',
      eventType: 'Music',
      totalSeats: '120',
      availableSeats: '48',
      price: '25.00',
    })
  })
})
