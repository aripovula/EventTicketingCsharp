import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { login } from '../api/auth'

const USERS = [
  { label: 'John Doe',     email: 'john@example.com' },
  { label: 'Jane Doer',    email: 'jane@example.com' },
  { label: 'Alex Johnson', email: 'alex@example.com' },
]

const ADMIN = { label: 'Admin', email: 'admin@example.com' }
const PASSWORD = 'Password'

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-cyan-400 transition'
const labelClass = 'block text-sm font-medium text-gray-700 mb-1'

interface PanelProps {
  title: string
  options: { label: string; email: string }[]
  selectedEmail: string
  onSelect: (email: string) => void
  onSubmit: () => void
  error: string | null
  submitting: boolean
  accent: 'gray' | 'cyan'
}

function DemoAccountPanel({ title, options, selectedEmail, onSelect, onSubmit, error, submitting, accent }: PanelProps) {
  const borderClass = accent === 'cyan'
    ? 'border-cyan-200 bg-cyan-50'
    : 'border-gray-200 bg-white'
  const buttonClass = accent === 'cyan'
    ? 'bg-cyan-600 hover:bg-cyan-700 text-white'
    : 'bg-gray-800 hover:bg-gray-900 text-white'

  return (
    <div className={`rounded-xl border p-8 flex flex-col gap-6 ${borderClass}`}>
      <h2 className="text-lg font-semibold text-gray-900 m-0">{title}</h2>

      <form onSubmit={e => { e.preventDefault(); onSubmit() }} className="flex flex-col gap-5">
        <fieldset className="flex flex-col gap-2 border-0 p-0 m-0">
          <legend className={labelClass}>Select account</legend>
          {options.map(opt => (
            <label key={opt.email} className="flex items-center gap-3 text-sm text-gray-700 cursor-pointer">
              <input
                type="radio"
                name={`user-${accent}`}
                value={opt.email}
                checked={selectedEmail === opt.email}
                onChange={() => onSelect(opt.email)}
                className="accent-cyan-600"
              />
              <span className="font-medium">{opt.label}</span>
              <span className="text-gray-400">{opt.email}</span>
            </label>
          ))}
        </fieldset>

        <div>
          <label htmlFor={`email-${accent}`} className={labelClass}>Email</label>
          <input
            id={`email-${accent}`}
            type="email"
            value={selectedEmail}
            readOnly
            className={`${inputClass} bg-gray-50 text-gray-500 cursor-default`}
          />
        </div>

        <div>
          <label htmlFor={`password-${accent}`} className={labelClass}>Password</label>
          <input
            id={`password-${accent}`}
            type="password"
            value={PASSWORD}
            readOnly
            className={`${inputClass} bg-gray-50 text-gray-500 cursor-default`}
          />
        </div>

        {error && <p role="alert" className="text-sm text-red-500">{error}</p>}

        <button
          type="submit"
          disabled={submitting}
          className={`${buttonClass} px-5 py-2 rounded-lg text-sm font-medium transition-colors disabled:opacity-40 disabled:cursor-not-allowed`}
        >
          {submitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </div>
  )
}

interface AccountPanelProps {
  onSubmit: (email: string, password: string) => void
  error: string | null
  submitting: boolean
}

function AccountSignInPanel({ onSubmit, error, submitting }: AccountPanelProps) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  return (
    <div className="rounded-xl border border-gray-200 bg-white p-8 flex flex-col gap-6">
      <h2 className="text-lg font-semibold text-gray-900 m-0">Sign in with your account</h2>

      <form onSubmit={e => { e.preventDefault(); onSubmit(email, password) }} className="flex flex-col gap-5">
        <div>
          <label htmlFor="email-account" className={labelClass}>Email</label>
          <input
            id="email-account"
            type="email"
            required
            value={email}
            onChange={e => setEmail(e.target.value)}
            className={inputClass}
          />
        </div>

        <div>
          <label htmlFor="password-account" className={labelClass}>Password</label>
          <input
            id="password-account"
            type="password"
            required
            value={password}
            onChange={e => setPassword(e.target.value)}
            className={inputClass}
          />
        </div>

        {error && <p role="alert" className="text-sm text-red-500">{error}</p>}

        <button
          type="submit"
          disabled={submitting}
          className="bg-gray-800 hover:bg-gray-900 text-white px-5 py-2 rounded-lg text-sm font-medium transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
        >
          {submitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </div>
  )
}

export default function LoginPage() {
  const [userEmail, setUserEmail] = useState(USERS[0].email)
  const [adminEmail, setAdminEmail] = useState(ADMIN.email)
  const [userError, setUserError] = useState<string | null>(null)
  const [adminError, setAdminError] = useState<string | null>(null)
  const [userBusy, setUserBusy] = useState(false)
  const [adminBusy, setAdminBusy] = useState(false)
  const [accountError, setAccountError] = useState<string | null>(null)
  const [accountBusy, setAccountBusy] = useState(false)
  const navigate = useNavigate()

  async function handleLogin(
    email: string,
    password: string,
    setError: (error: string | null) => void,
    setBusy: (busy: boolean) => void,
  ) {
    setError(null)
    setBusy(true)
    try {
      const res = await login(email, password)
      if (res.ok) {
        navigate('/')
        return
      }
      const body = await res.json().catch(() => null)
      setError(body?.errors?.[0]?.detail ?? 'Login failed. Please try again.')
    } catch {
      setError('Network error. Is the server running?')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold text-gray-900">Sign in</h1>
      <div className="flex gap-6">
        <div className="w-[55%]">
          <DemoAccountPanel
            title="Sign in as a regular user"
            options={USERS}
            selectedEmail={userEmail}
            onSelect={setUserEmail}
            onSubmit={() => handleLogin(userEmail, PASSWORD, setUserError, setUserBusy)}
            error={userError}
            submitting={userBusy}
            accent="gray"
          />
        </div>
        <div className="w-[45%]">
          <DemoAccountPanel
            title="Sign in as admin"
            options={[ADMIN]}
            selectedEmail={adminEmail}
            onSelect={setAdminEmail}
            onSubmit={() => handleLogin(adminEmail, PASSWORD, setAdminError, setAdminBusy)}
            error={adminError}
            submitting={adminBusy}
            accent="cyan"
          />
        </div>
      </div>
      <AccountSignInPanel
        onSubmit={(email, password) => handleLogin(email, password, setAccountError, setAccountBusy)}
        error={accountError}
        submitting={accountBusy}
      />
    </div>
  )
}
