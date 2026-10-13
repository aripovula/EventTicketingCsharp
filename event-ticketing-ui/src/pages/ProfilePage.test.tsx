import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { AuthContext, type User } from '../context/authContext'
import ProfilePage from './ProfilePage'

const john: User = { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }

const visa = { brand: 'visa', last4: '4242', expMonth: 12, expYear: 2029 }

function renderPage(user: User | null = john) {
  render(
    <AuthContext.Provider value={{ user, loading: false, signIn: () => {}, signOut: async () => {} }}>
      <MemoryRouter><ProfilePage /></MemoryRouter>
    </AuthContext.Provider>,
  )
}

const json = (body: unknown, status = 200) => Promise.resolve(new Response(JSON.stringify(body), { status }))

function stubApi({ saved = null as typeof visa | null, put = () => json({ data: visa, errors: [] }) } = {}) {
  const fetchMock = vi.fn((_url: string, init?: RequestInit) =>
    init?.method === 'PUT'
      ? put()
      : saved ? json({ data: saved, errors: [] }) : json({ data: null, errors: [] }, 404))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

describe('ProfilePage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('asks signed-out visitors to sign in', () => {
    stubApi()
    renderPage(null)

    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/login')
  })

  it('shows the account name and email', () => {
    stubApi()
    renderPage()

    expect(screen.getByText('John Doe')).toBeInTheDocument()
    expect(screen.getByText('john@example.com')).toBeInTheDocument()
  })

  it('shows the saved card', async () => {
    stubApi({ saved: visa })
    renderPage()

    expect(await screen.findByText('Visa ending in 4242')).toBeInTheDocument()
    expect(screen.getByText('Expires 12/2029')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Replace card' })).toBeInTheDocument()
  })

  it('says when no card is saved', async () => {
    stubApi()
    renderPage()

    expect(await screen.findByText('No saved card yet.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save card' })).toBeInTheDocument()
  })

  it('saves the chosen test card and shows it', async () => {
    const fetchMock = stubApi({ put: () => json({ data: { ...visa, last4: '0002' }, errors: [] }) })
    renderPage()

    await userEvent.click(await screen.findByLabelText('Visa 4000 0000 0000 0002 (declined)'))
    await userEvent.click(screen.getByRole('button', { name: 'Save card' }))

    expect(await screen.findByRole('status')).toHaveTextContent('Card saved.')
    expect(screen.getByText('Visa ending in 0002')).toBeInTheDocument()
    const put = fetchMock.mock.calls.find(([, init]) => init?.method === 'PUT')!
    expect(put[1]!.body).toBe(JSON.stringify({ paymentMethodId: 'pm_card_chargeDeclined' }))
  })

  it("shows Stripe's reason when the card is refused", async () => {
    stubApi({ put: () => json({ data: null, errors: [{ code: 'payment_declined', detail: 'Your card was declined.' }] }, 402) })
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: 'Save card' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Your card was declined.')
  })

  it('reports other failures', async () => {
    stubApi({ put: () => json({}, 500) })
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: 'Save card' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Saving the card failed. Please try again.')
  })
})
