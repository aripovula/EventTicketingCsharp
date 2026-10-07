import { render, screen } from '@testing-library/react'
import LoginPage from './LoginPage'

describe('LoginPage', () => {
  it('renders the sign-in heading', () => {
    render(<LoginPage />)

    expect(screen.getByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
  })
})
