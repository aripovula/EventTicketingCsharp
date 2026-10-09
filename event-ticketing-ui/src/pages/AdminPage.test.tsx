import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import type { EventItem } from '../api/events'
import { makeEvent } from '../test/fixtures'
import AdminPage from './AdminPage'

class FakeIntersectionObserver {
  observe() {}
  disconnect() {}
}

function stubEvents(events: EventItem[]) {
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(JSON.stringify({ data: events, errors: [], meta: { nextCursor: null } })))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function renderPage() {
  render(<MemoryRouter><AdminPage /></MemoryRouter>)
}

describe('AdminPage', () => {
  beforeEach(() => vi.stubGlobal('IntersectionObserver', FakeIntersectionObserver))
  afterEach(() => vi.unstubAllGlobals())

  it('renders the admin heading and a new-event link', async () => {
    stubEvents([])
    renderPage()

    expect(screen.getByRole('heading', { name: 'Admin' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '+ New event' })).toHaveAttribute('href', '/admin/new')
  })

  it('lists events by name with their venue and an edit link', async () => {
    const fetchMock = stubEvents([makeEvent(), makeEvent({ id: 2, title: 'Comedy Showcase', venue: 'Laugh Factory' })])
    renderPage()

    const list = await screen.findByRole('list', { name: 'admin events' })
    expect(within(list).getAllByRole('listitem')).toHaveLength(2)
    expect(within(list).getByText('Laugh Factory')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Edit Comedy Showcase' })).toHaveAttribute('href', '/admin/2/edit')
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events?sort=name')
  })
})
