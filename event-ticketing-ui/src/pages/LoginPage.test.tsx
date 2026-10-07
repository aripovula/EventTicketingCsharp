import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import LoginPage from './LoginPage'

describe('LoginPage', () => {
  it('renders the sign-in heading', () => {
    render(<LoginPage />)

    expect(screen.getByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
  })

  it('renders a regular-user panel and an admin panel', () => {
    render(<LoginPage />)

    expect(screen.getByRole('heading', { name: 'Sign in as a regular user' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Sign in as admin' })).toBeInTheDocument()
  })

  it('offers the three demo users and the admin as radio options', () => {
    render(<LoginPage />)

    for (const name of [/john doe/i, /jane doer/i, /alex johnson/i, /admin/i]) {
      expect(screen.getByRole('radio', { name })).toBeInTheDocument()
    }
  })

  it('preselects John Doe and fills the read-only email field', () => {
    render(<LoginPage />)

    expect(screen.getByRole('radio', { name: /john doe/i })).toBeChecked()
    expect(screen.getAllByLabelText('Email')[0]).toHaveValue('john@example.com')
  })

  it('updates the email field when another demo user is picked', async () => {
    render(<LoginPage />)

    await userEvent.click(screen.getByRole('radio', { name: /jane doer/i }))

    expect(screen.getAllByLabelText('Email')[0]).toHaveValue('jane@example.com')
  })
})
