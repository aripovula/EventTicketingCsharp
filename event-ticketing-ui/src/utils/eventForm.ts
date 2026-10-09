import type { EventInput, EventItem } from '../api/events'

export interface EventFormData {
  title: string
  venue: string
  description: string
  imageUrl: string
  startTime: string
  endTime: string
  eventType: string
  totalSeats: string
  availableSeats: string
  price: string
}

/** A datetime-local value ("2026-11-02T19:00", the admin's local time) as a UTC ISO timestamp. */
export function localInputToIso(value: string): string {
  return new Date(value).toISOString()
}

/** Dollars typed into the form as integer cents, rounding away float noise (19.99 → 1999). */
export function dollarsToCents(dollars: string): number {
  return Math.round(Number(dollars) * 100)
}

export function toEventInput(form: EventFormData): EventInput {
  return {
    title: form.title,
    venue: form.venue,
    description: form.description,
    imageUrl: form.imageUrl.trim() || null,
    startTime: localInputToIso(form.startTime),
    endTime: localInputToIso(form.endTime),
    eventType: form.eventType,
    totalSeats: Number(form.totalSeats),
    availableSeats: Number(form.availableSeats),
    priceCents: dollarsToCents(form.price),
  }
}

/** A UTC ISO timestamp as a datetime-local value in the admin's local time. */
export function isoToLocalInput(iso: string): string {
  const d = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

export function centsToDollars(cents: number): string {
  return (cents / 100).toFixed(2)
}

export function eventToForm(event: EventItem): EventFormData {
  return {
    title: event.title,
    venue: event.venue,
    description: event.description,
    imageUrl: event.imageUrl ?? '',
    startTime: isoToLocalInput(event.startTime),
    endTime: isoToLocalInput(event.endTime),
    eventType: event.eventType,
    totalSeats: String(event.totalSeats),
    availableSeats: String(event.availableSeats),
    price: centsToDollars(event.priceCents),
  }
}
