import { render, screen } from '@testing-library/react'
import RegisterPage from './RegisterPage'

describe('RegisterPage', () => {
  it('renders the create-account heading', () => {
    render(<RegisterPage />)

    expect(screen.getByRole('heading', { name: 'Create an account' })).toBeInTheDocument()
  })
})
