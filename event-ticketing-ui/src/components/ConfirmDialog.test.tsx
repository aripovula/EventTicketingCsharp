import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ConfirmDialog from './ConfirmDialog'

function renderDialog(overrides: Partial<React.ComponentProps<typeof ConfirmDialog>> = {}) {
  const props = {
    title: 'Delete item?',
    message: 'This cannot be undone.',
    onConfirm: vi.fn(),
    onCancel: vi.fn(),
    ...overrides,
  }
  render(<ConfirmDialog {...props} />)
  return props
}

test('renders title and message', () => {
  renderDialog()
  expect(screen.getByRole('heading', { name: 'Delete item?' })).toBeInTheDocument()
  expect(screen.getByText('This cannot be undone.')).toBeInTheDocument()
})

test('renders with role dialog', () => {
  renderDialog()
  expect(screen.getByRole('dialog')).toBeInTheDocument()
})

test('uses custom confirmLabel on the confirm button', () => {
  renderDialog({ confirmLabel: 'Remove' })
  expect(screen.getByRole('button', { name: 'Remove' })).toBeInTheDocument()
})

test('defaults confirm button label to Confirm', () => {
  renderDialog()
  expect(screen.getByRole('button', { name: 'Confirm' })).toBeInTheDocument()
})

test('calls onConfirm when confirm button is clicked', async () => {
  const user = userEvent.setup()
  const { onConfirm } = renderDialog({ confirmLabel: 'Delete' })
  await user.click(screen.getByRole('button', { name: 'Delete' }))
  expect(onConfirm).toHaveBeenCalledTimes(1)
})

test('calls onCancel when Cancel button is clicked', async () => {
  const user = userEvent.setup()
  const { onCancel } = renderDialog()
  await user.click(screen.getByRole('button', { name: 'Cancel' }))
  expect(onCancel).toHaveBeenCalledTimes(1)
})
