import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import RegisterPage from './RegisterPage'

function LoginProbe() {
  const { state } = useLocation()
  return <p>Login page{state?.registered ? ' after registering' : ''}</p>
}

function renderPage() {
  render(
    <MemoryRouter initialEntries={['/register']}>
      <Routes>
        <Route path="/login" element={<LoginProbe />} />
        <Route path="/register" element={<RegisterPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

async function fillAndSubmit() {
  await userEvent.type(screen.getByLabelText('Name'), 'New User')
  await userEvent.type(screen.getByLabelText('Email'), 'new.user@example.com')
  await userEvent.type(screen.getByLabelText('Password'), 'secret-password')
  await userEvent.click(screen.getByRole('button', { name: 'Create account' }))
}

describe('RegisterPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('renders the create-account heading', () => {
    renderPage()

    expect(screen.getByRole('heading', { name: 'Create an account' })).toBeInTheDocument()
  })

  it('requires a password of at least 8 characters', () => {
    renderPage()

    expect(screen.getByLabelText('Password')).toHaveAttribute('minLength', '8')
    expect(screen.getByLabelText('Password')).toHaveAccessibleDescription('At least 8 characters.')
  })

  it('creates the account and goes to the login page', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{}', { status: 201 }))
    vi.stubGlobal('fetch', fetchMock)
    renderPage()

    await fillAndSubmit()

    expect(JSON.parse(fetchMock.mock.calls[0][1].body)).toEqual(
      { name: 'New User', email: 'new.user@example.com', password: 'secret-password' })
    expect(await screen.findByText('Login page after registering')).toBeInTheDocument()
  })

  it('stays on the register page when registration fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 409 })))
    renderPage()

    await fillAndSubmit()

    expect(await screen.findByRole('button', { name: 'Create account' })).toBeEnabled()
    expect(screen.queryByText(/^Login page/)).not.toBeInTheDocument()
  })

  function stubRegister(status: number, errors: { code: string; detail: string; field?: string }[]) {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ data: null, errors }), { status })))
  }

  it('shows a taken email under the email field', async () => {
    stubRegister(409, [{ code: 'email_taken', detail: 'An account with this email already exists.', field: 'email' }])
    renderPage()

    await fillAndSubmit()

    expect(await screen.findByText('An account with this email already exists.')).toBeInTheDocument()
    expect(screen.getByLabelText('Email')).toHaveAttribute('aria-invalid', 'true')
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('shows validation errors under each named field', async () => {
    stubRegister(400, [
      { code: 'validation_failed', detail: 'Name is too short.', field: 'name' },
      { code: 'validation_failed', detail: 'Password is too short.', field: 'password' },
    ])
    renderPage()

    await fillAndSubmit()

    expect(await screen.findByText('Name is too short.')).toBeInTheDocument()
    expect(screen.getByText('Password is too short.')).toBeInTheDocument()
    expect(screen.getByLabelText('Name')).toHaveAttribute('aria-invalid', 'true')
  })

  it('shows errors without a field in an alert', async () => {
    stubRegister(500, [{ code: 'internal_error', detail: 'An unexpected error occurred.' }])
    renderPage()

    await fillAndSubmit()

    expect(await screen.findByRole('alert')).toHaveTextContent('An unexpected error occurred.')
  })

  it('falls back to a generic message when the body has no errors', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('oops', { status: 502 })))
    renderPage()

    await fillAndSubmit()

    expect(await screen.findByRole('alert')).toHaveTextContent('Registration failed. Please try again.')
  })

  it('shows a network error when the request fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))
    renderPage()

    await fillAndSubmit()

    expect(await screen.findByRole('alert')).toHaveTextContent('Network error. Is the server running?')
  })
})
