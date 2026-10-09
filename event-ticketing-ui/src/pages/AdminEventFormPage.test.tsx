import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { makeEvent } from '../test/fixtures'
import AdminEventFormPage from './AdminEventFormPage'

function renderPage() {
  render(
    <MemoryRouter initialEntries={['/admin/new']}>
      <Routes>
        <Route path="/admin" element={<p>Admin page</p>} />
        <Route path="/admin/new" element={<AdminEventFormPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

async function fillForm() {
  await userEvent.type(screen.getByLabelText('Title'), 'Brand New Gig')
  await userEvent.type(screen.getByLabelText('Venue'), 'Main Hall')
  await userEvent.type(screen.getByLabelText('Description'), 'A new gig.')
  await userEvent.type(screen.getByLabelText('Start time'), '2026-11-02T19:00')
  await userEvent.type(screen.getByLabelText('End time'), '2026-11-02T22:00')
  await userEvent.type(screen.getByLabelText('Total seats'), '100')
  await userEvent.type(screen.getByLabelText('Available seats'), '100')
  await userEvent.type(screen.getByLabelText('Price ($)'), '19.99')
}

// happy-dom's constraint validation wrongly rejects any price with step 0.01 (it checks the step in
// floating point), so these tests submit the form directly; real-browser validation is covered by E2E.
function submitForm() {
  fireEvent.submit(screen.getByRole('form', { name: 'event form' }))
}

describe('AdminEventFormPage', () => {
  it('renders the new-event heading and a link back to admin', () => {
    renderPage()

    expect(screen.getByRole('heading', { name: 'New event' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '← Back to admin' })).toHaveAttribute('href', '/admin')
  })

  it('has every event field', () => {
    renderPage()

    for (const label of ['Title', 'Venue', 'Description', 'Image URL', 'Start time', 'End time',
      'Event type', 'Total seats', 'Available seats', 'Price ($)']) {
      expect(screen.getByLabelText(label)).toBeInTheDocument()
    }
  })

  it('offers every event type, defaulting to Music', () => {
    renderPage()

    const select = screen.getByLabelText('Event type')
    expect(select).toHaveValue('Music')
    expect(screen.getAllByRole('option')).toHaveLength(9)
  })

  it('keeps what the admin types', async () => {
    renderPage()

    await userEvent.type(screen.getByLabelText('Title'), 'Brand New Gig')
    await userEvent.type(screen.getByLabelText('Price ($)'), '19.99')

    expect(screen.getByLabelText('Title')).toHaveValue('Brand New Gig')
    expect(screen.getByLabelText('Price ($)')).toHaveValue(19.99)
  })

  it('offers create and cancel actions', () => {
    renderPage()

    expect(screen.getByRole('button', { name: 'Create event' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Cancel' })).toHaveAttribute('href', '/admin')
  })

  it('creates the event and returns to the admin page', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 201 }))
    vi.stubGlobal('fetch', fetchMock)
    renderPage()

    await fillForm()
    submitForm()

    expect(await screen.findByText('Admin page')).toBeInTheDocument()
    const body = JSON.parse(fetchMock.mock.calls[0][1].body)
    expect(body).toMatchObject({ title: 'Brand New Gig', priceCents: 1999, totalSeats: 100, imageUrl: null })
    expect(body.startTime).toBe(new Date(2026, 10, 2, 19, 0).toISOString())
  })

  it('shows the server errors when the event is rejected', async () => {
    const envelope = { data: null, errors: [{ code: 'validation_failed', detail: 'End time must be after start time.', field: 'endTime' }] }
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(envelope), { status: 400 })))
    renderPage()

    await fillForm()
    submitForm()

    expect(await screen.findByRole('alert')).toHaveTextContent('End time must be after start time.')
    expect(screen.queryByText('Admin page')).not.toBeInTheDocument()
  })

  it('falls back to a generic message for other failures', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('oops', { status: 500 })))
    renderPage()

    await fillForm()
    submitForm()

    expect(await screen.findByRole('alert')).toHaveTextContent('Failed to create event (500). Please try again.')
  })

  function renderEdit(id = 7) {
    render(
      <MemoryRouter initialEntries={[`/admin/${id}/edit`]}>
        <Routes>
          <Route path="/admin" element={<p>Admin page</p>} />
          <Route path="/admin/:id/edit" element={<AdminEventFormPage />} />
        </Routes>
      </MemoryRouter>,
    )
  }

  it('loads the event into the form when editing', async () => {
    const event = makeEvent({ id: 7, startTime: new Date(2026, 10, 2, 19, 0).toISOString(), priceCents: 1999 })
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ data: event, errors: [] })))
    vi.stubGlobal('fetch', fetchMock)
    renderEdit()

    expect(await screen.findByRole('heading', { name: 'Edit event' })).toBeInTheDocument()
    expect(screen.getByLabelText('Title')).toHaveValue('Jazz Night')
    expect(screen.getByLabelText('Start time')).toHaveValue('2026-11-02T19:00')
    expect(screen.getByLabelText('Price ($)')).toHaveValue(19.99)
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/events/7')
  })

  it('says when the event cannot be loaded', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 404 })))
    renderEdit()

    expect(await screen.findByText('Failed to load event (404).')).toBeInTheDocument()
  })

  function stubEditThenSave(saveResponse: Response) {
    const event = makeEvent({ id: 7 })
    const fetchMock = vi.fn((_url: string, init?: RequestInit) => Promise.resolve(init?.method === 'PUT'
      ? saveResponse
      : new Response(JSON.stringify({ data: event, errors: [] }))))
    vi.stubGlobal('fetch', fetchMock)
    return fetchMock
  }

  it('saves changes to the edited event and returns to admin', async () => {
    const fetchMock = stubEditThenSave(new Response(null, { status: 204 }))
    renderEdit()
    const title = await screen.findByLabelText('Title')

    await userEvent.clear(title)
    await userEvent.type(title, 'Jazz Night Deluxe')
    expect(screen.getByRole('button', { name: 'Save changes' })).toBeInTheDocument()
    submitForm()

    expect(await screen.findByText('Admin page')).toBeInTheDocument()
    const [url, init] = fetchMock.mock.calls.find(([, i]) => i?.method === 'PUT')!
    expect(url).toBe('/api/v1/events/7')
    expect(JSON.parse(init!.body as string).title).toBe('Jazz Night Deluxe')
  })

  it('reports a failed update', async () => {
    stubEditThenSave(new Response('oops', { status: 500 }))
    renderEdit()
    await screen.findByLabelText('Title')

    submitForm()

    expect(await screen.findByRole('alert')).toHaveTextContent('Failed to update event (500). Please try again.')
  })
})
