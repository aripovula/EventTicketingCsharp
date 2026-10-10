import { act, render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import AdminSummaryPage from './AdminSummaryPage'

const hub = vi.hoisted(() => ({ handlers: {} as Record<string, (eventId: number) => void> }))
vi.mock('../hooks/useHubEvents', () => ({
  useHubEvents: (handlers: Record<string, (eventId: number) => void>) => { hub.handlers = handlers },
}))

function renderPage() {
  render(<MemoryRouter><AdminSummaryPage /></MemoryRouter>)
}

describe('AdminSummaryPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('shows seats and revenue per event', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: [
      { eventId: 1, title: 'Jazz Night', totalSeats: 120, soldSeats: 72, remainingSeats: 48, revenueCents: 180000 },
    ], errors: [] }))))
    renderPage()

    const row = within(await screen.findByRole('table')).getAllByRole('row')[1]
    expect(row).toHaveTextContent('Jazz Night')
    expect(row).toHaveTextContent('120')
    expect(row).toHaveTextContent('72')
    expect(row).toHaveTextContent('48')
    expect(row).toHaveTextContent('$1,800.00')
    expect(screen.getByText(/Data as of/)).toBeInTheDocument()
  })

  it('says when the summary cannot be loaded', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 500 })))
    renderPage()

    expect(await screen.findByText('Failed to load summary.')).toBeInTheDocument()
  })

  it('recounts when a booking is made', async () => {
    const summary = (soldSeats: number) => new Response(JSON.stringify({ data: [
      { eventId: 1, title: 'Jazz Night', totalSeats: 120, soldSeats, remainingSeats: 120 - soldSeats, revenueCents: soldSeats * 2500 },
    ], errors: [] }))
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(summary(72)).mockResolvedValueOnce(summary(73)))
    renderPage()
    await screen.findByText('$1,800.00')

    act(() => hub.handlers.BookingMade(1))

    expect(await screen.findByText('$1,825.00')).toBeInTheDocument()
  })
})
