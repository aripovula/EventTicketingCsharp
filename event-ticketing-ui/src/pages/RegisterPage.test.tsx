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
})
