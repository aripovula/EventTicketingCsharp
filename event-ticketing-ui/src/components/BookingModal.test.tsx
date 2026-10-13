import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import BookingModal from './BookingModal'

function renderModal(overrides: Partial<React.ComponentProps<typeof BookingModal>> = {}) {
  const props = {
    eventTitle: 'Jazz Night',
    eventDate: '2026-11-02T23:00:00Z',
    eventVenue: 'Blue Note Club',
    priceCents: 2500,
    onConfirm: vi.fn().mockResolvedValue(undefined),
    onClose: vi.fn(),
    error: null,
    ...overrides,
  }
  render(<BookingModal {...props} />)
  return props
}

describe('BookingModal', () => {
  it('summarises the event and price', () => {
    renderModal()

    expect(screen.getByRole('dialog', { name: 'Place order' })).toHaveTextContent('Jazz Night · Blue Note Club')
    expect(screen.getByText('$25.00')).toBeInTheDocument()
  })

  it('prefills the email of a signed-in user', () => {
    renderModal({ savedEmail: 'john@example.com' })

    expect(screen.getByLabelText('Email')).toHaveValue('john@example.com')
  })

  it('confirms with the typed email and the approved test card', async () => {
    const props = renderModal()

    await userEvent.type(screen.getByLabelText('Email'), 'guest@example.com')
    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))

    expect(props.onConfirm).toHaveBeenCalledWith('guest@example.com', { paymentMethodId: 'pm_card_visa' })
  })

  it('pays with the declining test card when it is chosen', async () => {
    const props = renderModal({ savedEmail: 'john@example.com' })

    await userEvent.click(screen.getByLabelText('Visa 4000 0000 0000 0002 (declined)'))
    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))

    expect(props.onConfirm).toHaveBeenCalledWith('john@example.com', { paymentMethodId: 'pm_card_chargeDeclined' })
  })

  it('disables the buttons while the order is being placed', async () => {
    let finish: () => void = () => {}
    renderModal({ savedEmail: 'john@example.com', onConfirm: vi.fn(() => new Promise<void>(resolve => { finish = resolve })) })

    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))

    expect(screen.getByRole('button', { name: 'Placing order…' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
    finish()
  })

  it('shows the error it is given', () => {
    renderModal({ error: 'Sorry, this event just sold out.' })

    expect(screen.getByRole('alert')).toHaveTextContent('Sorry, this event just sold out.')
  })

  it('books a free event without asking for a card', async () => {
    const props = renderModal({ priceCents: 0, savedEmail: 'john@example.com' })

    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
    expect(screen.getByText('This event is free, so no card is needed.')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))

    expect(props.onConfirm).toHaveBeenCalledWith('john@example.com', null)
  })

  const savedCard = { brand: 'visa', last4: '4242', expMonth: 12, expYear: 2029 }

  it('offers the saved card first and pays with it', async () => {
    const props = renderModal({ savedEmail: 'john@example.com', savedCard })

    expect(screen.getByLabelText('Visa ending in 4242 (expires 12/2029)')).toBeChecked()
    expect(screen.queryByText('Test card')).not.toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))

    expect(props.onConfirm).toHaveBeenCalledWith('john@example.com', { useSavedPaymentMethod: true })
  })

  it('pays with a different card when asked', async () => {
    const props = renderModal({ savedEmail: 'john@example.com', savedCard })

    await userEvent.click(screen.getByLabelText('A different card'))
    await userEvent.click(screen.getByLabelText('Visa 4000 0000 0000 0002 (declined)'))
    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))

    expect(props.onConfirm).toHaveBeenCalledWith('john@example.com', { paymentMethodId: 'pm_card_chargeDeclined' })
  })

  it('does not offer the saved card for a free event', () => {
    renderModal({ priceCents: 0, savedCard })

    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
  })

  it('cancels without confirming', async () => {
    const props = renderModal()

    await userEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(props.onClose).toHaveBeenCalled()
    expect(props.onConfirm).not.toHaveBeenCalled()
  })
})
