import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import AdminSummaryPage from './AdminSummaryPage'

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
})
