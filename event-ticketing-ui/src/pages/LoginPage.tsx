import { useState } from 'react'

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
  accent: 'gray' | 'cyan'
}

function DemoAccountPanel({ title, options, selectedEmail, onSelect, accent }: PanelProps) {
  const borderClass = accent === 'cyan'
    ? 'border-cyan-200 bg-cyan-50'
    : 'border-gray-200 bg-white'
  const buttonClass = accent === 'cyan'
    ? 'bg-cyan-600 hover:bg-cyan-700 text-white'
    : 'bg-gray-800 hover:bg-gray-900 text-white'

  return (
    <div className={`rounded-xl border p-8 flex flex-col gap-6 ${borderClass}`}>
      <h2 className="text-lg font-semibold text-gray-900 m-0">{title}</h2>

      <form onSubmit={e => e.preventDefault()} className="flex flex-col gap-5">
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

        <button
          type="submit"
          className={`${buttonClass} px-5 py-2 rounded-lg text-sm font-medium transition-colors`}
        >
          Sign in
        </button>
      </form>
    </div>
  )
}

export default function LoginPage() {
  const [userEmail, setUserEmail] = useState(USERS[0].email)
  const [adminEmail, setAdminEmail] = useState(ADMIN.email)

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
            accent="gray"
          />
        </div>
        <div className="w-[45%]">
          <DemoAccountPanel
            title="Sign in as admin"
            options={[ADMIN]}
            selectedEmail={adminEmail}
            onSelect={setAdminEmail}
            accent="cyan"
          />
        </div>
      </div>
    </div>
  )
}
