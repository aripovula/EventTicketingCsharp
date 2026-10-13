import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import BookingModal from './BookingModal'

// With a publishable key the modal shows Stripe's card field; Stripe itself is mocked here.
const createPaymentMethod = vi.fn()
vi.mock('../payments/stripe', () => ({ stripePromise: Promise.resolve({}), TEST_CARDS: [] }))
vi.mock('@stripe/react-stripe-js', () => ({
  Elements: ({ children }: { children: React.ReactNode }) => children,
  CardElement: () => <div data-testid="stripe-card" />,
  useStripe: () => ({ createPaymentMethod }),
  useElements: () => ({ getElement: () => ({}) }),
}))

function renderModal() {
  const props = {
    eventTitle: 'Jazz Night',
    eventDate: '2026-11-02T23:00:00Z',
    eventVenue: 'Blue Note Club',
    priceCents: 2500,
    savedEmail: 'john@example.com',
    onConfirm: vi.fn().mockResolvedValue(undefined),
    onClose: vi.fn(),
    error: null,
  }
  render(<BookingModal {...props} />)
  return props
}

describe('BookingModal with Stripe', () => {
  it("shows Stripe's card field instead of the test cards", () => {
    renderModal()

    expect(screen.getByTestId('stripe-card')).toBeInTheDocument()
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
  })

  it('confirms with the PaymentMethod Stripe created from the card', async () => {
    createPaymentMethod.mockResolvedValue({ paymentMethod: { id: 'pm_123' } })
    const props = renderModal()

    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))

    expect(createPaymentMethod).toHaveBeenCalledWith({ type: 'card', card: {} })
    expect(props.onConfirm).toHaveBeenCalledWith('john@example.com', { paymentMethodId: 'pm_123' })
  })

  it("shows Stripe's card error without placing the order", async () => {
    createPaymentMethod.mockResolvedValue({ error: { message: 'Your card number is incomplete.' } })
    const props = renderModal()

    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Your card number is incomplete.')
    expect(props.onConfirm).not.toHaveBeenCalled()
  })
})
