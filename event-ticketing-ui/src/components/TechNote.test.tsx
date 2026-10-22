import { render, screen, within } from '@testing-library/react'
import TechNote from './TechNote'

describe('TechNote', () => {
  it('lists each point under a heading', () => {
    render(<TechNote points={['Uses SignalR.', <><strong>Cached</strong> in Redis.</>]} />)

    const note = screen.getByRole('complementary', { name: 'How this page works' })
    expect(within(note).getByText('How this page works')).toBeInTheDocument()
    const items = within(note).getAllByRole('listitem')
    expect(items.map(item => item.textContent)).toEqual(['Uses SignalR.', 'Cached in Redis.'])
  })
})
