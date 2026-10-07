import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import LoginPage from './LoginPage'

function renderPage() {
  render(
    <MemoryRouter initialEntries={['/login']}>
      <Routes>
        <Route path="/" element={<p>Home page</p>} />
        <Route path="/login" element={<LoginPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('LoginPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('renders the sign-in heading', () => {
    renderPage()

    expect(screen.getByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
  })

  it('renders a regular-user panel and an admin panel', () => {
    renderPage()

    expect(screen.getByRole('heading', { name: 'Sign in as a regular user' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Sign in as admin' })).toBeInTheDocument()
  })

  it('offers the three demo users and the admin as radio options', () => {
    renderPage()

    for (const name of [/john doe/i, /jane doer/i, /alex johnson/i, /admin/i]) {
      expect(screen.getByRole('radio', { name })).toBeInTheDocument()
    }
  })

  it('preselects John Doe and fills the read-only email field', () => {
    renderPage()

    expect(screen.getByRole('radio', { name: /john doe/i })).toBeChecked()
    expect(screen.getAllByLabelText('Email')[0]).toHaveValue('john@example.com')
  })

  it('updates the email field when another demo user is picked', async () => {
    renderPage()

    await userEvent.click(screen.getByRole('radio', { name: /jane doer/i }))

    expect(screen.getAllByLabelText('Email')[0]).toHaveValue('jane@example.com')
  })

  it('signs in as the selected demo user and goes home', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)
    renderPage()

    await userEvent.click(screen.getByRole('radio', { name: /jane doer/i }))
    await userEvent.click(screen.getAllByRole('button', { name: 'Sign in' })[0])

    expect(JSON.parse(fetchMock.mock.calls[0][1].body)).toEqual({ email: 'jane@example.com', password: 'Password' })
    expect(await screen.findByText('Home page')).toBeInTheDocument()
  })

  it('signs in as admin from the admin panel', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)
    renderPage()

    await userEvent.click(screen.getAllByRole('button', { name: 'Sign in' })[1])

    expect(JSON.parse(fetchMock.mock.calls[0][1].body).email).toBe('admin@example.com')
  })

  it('stays on the login page when sign-in fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 401 })))
    renderPage()

    await userEvent.click(screen.getAllByRole('button', { name: 'Sign in' })[0])

    expect(screen.queryByText('Home page')).not.toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
  })

  it('shows the error detail from the response envelope', async () => {
    const envelope = { data: null, errors: [{ code: 'invalid_credentials', detail: 'Invalid email or password.' }] }
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(envelope), { status: 401 })))
    renderPage()

    await userEvent.click(screen.getAllByRole('button', { name: 'Sign in' })[0])

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid email or password.')
  })

  it('falls back to a generic message when the body has no error detail', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('oops', { status: 500 })))
    renderPage()

    await userEvent.click(screen.getAllByRole('button', { name: 'Sign in' })[0])

    expect(await screen.findByRole('alert')).toHaveTextContent('Login failed. Please try again.')
  })

  it('shows a network error when the request fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))
    renderPage()

    await userEvent.click(screen.getAllByRole('button', { name: 'Sign in' })[1])

    expect(await screen.findByRole('alert')).toHaveTextContent('Network error. Is the server running?')
  })

  it('disables the button and shows progress while signing in', async () => {
    let respond: (response: Response) => void = () => {}
    vi.stubGlobal('fetch', vi.fn().mockReturnValue(new Promise<Response>(resolve => { respond = resolve })))
    renderPage()

    await userEvent.click(screen.getAllByRole('button', { name: 'Sign in' })[0])

    const busyButton = screen.getByRole('button', { name: 'Signing in…' })
    expect(busyButton).toBeDisabled()
    respond(new Response('{}', { status: 401 }))
    await waitFor(() => expect(screen.getAllByRole('button', { name: 'Sign in' })).toHaveLength(3))
  })

  it('signs in with a typed email and password', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)
    renderPage()
    const panel = within(screen.getByRole('heading', { name: 'Sign in with your account' }).parentElement!)

    await userEvent.type(panel.getByLabelText('Email'), 'new.user@example.com')
    await userEvent.type(panel.getByLabelText('Password'), 'secret-password')
    await userEvent.click(panel.getByRole('button', { name: 'Sign in' }))

    expect(JSON.parse(fetchMock.mock.calls[0][1].body)).toEqual({ email: 'new.user@example.com', password: 'secret-password' })
    expect(await screen.findByText('Home page')).toBeInTheDocument()
  })

  it('shows account sign-in errors inside the account panel', async () => {
    const envelope = { data: null, errors: [{ code: 'invalid_credentials', detail: 'Invalid email or password.' }] }
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(envelope), { status: 401 })))
    renderPage()
    const panel = within(screen.getByRole('heading', { name: 'Sign in with your account' }).parentElement!)

    await userEvent.type(panel.getByLabelText('Email'), 'new.user@example.com')
    await userEvent.type(panel.getByLabelText('Password'), 'wrong-password')
    await userEvent.click(panel.getByRole('button', { name: 'Sign in' }))

    expect(await panel.findByRole('alert')).toHaveTextContent('Invalid email or password.')
  })
})
