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

  it('explains how the page works', () => {
    stubEvents([])
    renderPage()

    const note = screen.getByRole('complementary', { name: 'How this page works' })
    expect(note).toHaveTextContent('role-based authorization')
    expect(note).toHaveTextContent('SignalR')
    expect(note).toHaveTextContent('audit log')
    expect(note).toHaveTextContent('archived instead of deleted')
  })

  it('renders the admin heading', async () => {
    stubEvents([])
    renderPage()

    expect(screen.getByRole('heading', { name: 'Admin' })).toBeInTheDocument()
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

  function stubListWithOrders(actionResponse: Response) {
    const fetchMock = vi.fn((_url: string, init?: RequestInit) => Promise.resolve(init?.method
      ? actionResponse
      : new Response(JSON.stringify({ data: [makeEvent({ id: 2, title: 'Comedy Showcase', hasOrders: true })], errors: [], meta: { nextCursor: null } }))))
    vi.stubGlobal('fetch', fetchMock)
    return fetchMock
  }

  it('offers Archive instead of Delete for an event with orders', async () => {
    stubListWithOrders(new Response(null, { status: 204 }))
    renderPage()

    expect(await screen.findByRole('button', { name: 'Archive Comedy Showcase' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Delete Comedy Showcase' })).not.toBeInTheDocument()
  })

  it('archives the event after confirming and takes it off the list', async () => {
    const fetchMock = stubListWithOrders(new Response(null, { status: 204 }))
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: 'Archive Comedy Showcase' }))
    const dialog = screen.getByRole('dialog')
    expect(within(dialog).getByRole('heading', { name: 'Archive event?' })).toBeInTheDocument()
    expect(dialog).toHaveTextContent('its orders stay')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Archive' }))

    expect(screen.queryByText('Comedy Showcase')).not.toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events/2/archive', { method: 'POST', credentials: 'include' })
  })

  it('switches to Archive when a ticket was sold since the list loaded', async () => {
    stubListThenDelete(new Response(JSON.stringify({ data: null, errors: [{ code: 'has_orders' }] }), { status: 409 }))
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: 'Delete Comedy Showcase' }))
    await userEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Delete' }))

    expect(await screen.findByRole('alert')).toHaveTextContent("Tickets have been sold for this event, so it can't be deleted. Archive it instead.")
    expect(screen.getByRole('button', { name: 'Archive Comedy Showcase' })).toBeInTheDocument()
  })
})
