import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
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
    expect(screen.getByRole('link', { name: 'Events Calendar' })).toHaveAttribute('href', '/admin/calendar')
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

  function stubListThenDelete(deleteResponse: Response) {
    const fetchMock = vi.fn((_url: string, init?: RequestInit) => Promise.resolve(init?.method === 'DELETE'
      ? deleteResponse
      : new Response(JSON.stringify({ data: [makeEvent(), makeEvent({ id: 2, title: 'Comedy Showcase' })], errors: [], meta: { nextCursor: null } }))))
    vi.stubGlobal('fetch', fetchMock)
    return fetchMock
  }

  it('asks for confirmation before deleting', async () => {
    stubListThenDelete(new Response(null, { status: 204 }))
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: 'Delete Comedy Showcase' }))

    const dialog = screen.getByRole('dialog')
    expect(within(dialog).getByRole('heading', { name: 'Delete event?' })).toBeInTheDocument()
    expect(dialog).toHaveTextContent('"Comedy Showcase"')
  })

  it('removes the event after a confirmed delete', async () => {
    const fetchMock = stubListThenDelete(new Response(null, { status: 204 }))
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: 'Delete Comedy Showcase' }))
    await userEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Delete' }))

    expect(screen.queryByText('Comedy Showcase')).not.toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events/2', { method: 'DELETE', credentials: 'include' })
  })

  it('keeps the event when the delete is cancelled', async () => {
    const fetchMock = stubListThenDelete(new Response(null, { status: 204 }))
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: 'Delete Comedy Showcase' }))
    await userEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(screen.getByText('Comedy Showcase')).toBeInTheDocument()
    expect(fetchMock.mock.calls.some(([, init]) => init?.method === 'DELETE')).toBe(false)
  })

  it('shows an error and keeps the event when the delete fails', async () => {
    stubListThenDelete(new Response('{}', { status: 500 }))
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: 'Delete Comedy Showcase' }))
    await userEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Delete' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Failed to delete event (500). Please try again.')
    expect(screen.getByText('Comedy Showcase')).toBeInTheDocument()
  })
})
