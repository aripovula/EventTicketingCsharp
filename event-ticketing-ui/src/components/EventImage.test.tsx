import { fireEvent, render, screen } from '@testing-library/react'
import EventImage from './EventImage'

describe('EventImage', () => {
  it('shows the photo', () => {
    render(<EventImage src="https://example.com/a.jpg" alt="Jazz Night" />)

    expect(screen.getByRole('img', { name: 'Jazz Night' })).toHaveAttribute('src', 'https://example.com/a.jpg')
  })

  it('shows a placeholder when the photo fails to load', () => {
    render(<EventImage src="https://example.com/gone.jpg" alt="Jazz Night" className="h-64" />)

    fireEvent.error(screen.getByRole('img', { name: 'Jazz Night' }))

    const placeholder = screen.getByRole('img', { name: 'Jazz Night (image unavailable)' })
    expect(placeholder).toHaveTextContent('Image unavailable')
    expect(placeholder).toHaveClass('h-64')
  })

  it('tries again when the photo changes', () => {
    const { rerender } = render(<EventImage src="https://example.com/gone.jpg" alt="Jazz Night" />)
    fireEvent.error(screen.getByRole('img', { name: 'Jazz Night' }))

    rerender(<EventImage src="https://example.com/new.jpg" alt="Jazz Night" />)

    expect(screen.getByRole('img', { name: 'Jazz Night' })).toHaveAttribute('src', 'https://example.com/new.jpg')
  })
})
