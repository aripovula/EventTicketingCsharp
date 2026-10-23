import { render, screen } from '@testing-library/react'
import TechNote from './TechNote'

describe('TechNote', () => {
  it('reads as one paragraph: heading first, points joined by semicolons, then a full stop', () => {
    render(<TechNote points={['uses SignalR', <><strong>cached</strong> in Redis</>]} />)

    const note = screen.getByRole('complementary', { name: 'How this page works' })
    expect(note.querySelectorAll('p')).toHaveLength(1)
    expect(note.querySelector('li')).toBeNull()
    expect(note).toHaveTextContent(/^How this page works: uses SignalR; cached in Redis\.$/)
    expect(screen.getByText('How this page works:').tagName).toBe('STRONG')
  })
})
