import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import type { EventItem } from '../api/events'
import EventDetailPage from './EventDetailPage'
import { makeEvent } from '../test/fixtures'

function stubEvent(event: EventItem) {
  const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: event, errors: [] })))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function renderPage(id = 1) {
  render(
    <MemoryRouter initialEntries={[`/events/${id}`]}>
      <Routes><Route path="/events/:id" element={<EventDetailPage />} /></Routes>
    </MemoryRouter>,
  )
}

describe('EventDetailPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('links back to the events list', async () => {
    stubEvent(makeEvent())
    renderPage()

    expect(await screen.findByRole('link', { name: '← Back to events' })).toHaveAttribute('href', '/')
  })

  it('loads the event named in the URL', async () => {
    const fetchMock = stubEvent(makeEvent({ id: 7 }))
    renderPage(7)

    await screen.findByRole('heading', { name: 'Jazz Night' })
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events/7')
  })

  it('shows the event details, price and seats', async () => {
    stubEvent(makeEvent())
    renderPage()

    expect(await screen.findByRole('heading', { name: 'Jazz Night' })).toBeInTheDocument()
    expect(screen.getByText('Live jazz.')).toBeInTheDocument()
    expect(screen.getByText(/Blue Note Club/)).toBeInTheDocument()
    expect(screen.getByText('$25.00')).toBeInTheDocument()
    expect(screen.getByText('48 of 120 seats available')).toBeInTheDocument()
  })

  it('shows sold out when no seats are left', async () => {
    stubEvent(makeEvent({ availableSeats: 0 }))
    renderPage()

    expect(await screen.findByText('Sold out')).toBeInTheDocument()
  })

  it('names the browser tab after the event', async () => {
    stubEvent(makeEvent())
    renderPage()

    await screen.findByRole('heading', { name: 'Jazz Night' })
    expect(document.title).toBe('Jazz Night | Ticketing')
  })
})
