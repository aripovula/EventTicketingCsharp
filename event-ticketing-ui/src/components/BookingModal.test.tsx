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

    expect(props.onConfirm).toHaveBeenCalledWith('guest@example.com', 'pm_card_visa')
  })

  it('pays with the declining test card when it is chosen', async () => {
    const props = renderModal({ savedEmail: 'john@example.com' })

    await userEvent.click(screen.getByLabelText('Visa 4000 0000 0000 0002 (declined)'))
    await userEvent.click(screen.getByRole('button', { name: 'Place order' }))

    expect(props.onConfirm).toHaveBeenCalledWith('john@example.com', 'pm_card_chargeDeclined')
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

  it('cancels without confirming', async () => {
    const props = renderModal()

    await userEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(props.onClose).toHaveBeenCalled()
    expect(props.onConfirm).not.toHaveBeenCalled()
  })
})
