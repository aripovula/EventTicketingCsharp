import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { register } from '../api/auth'

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-cyan-400 transition'
const labelClass = 'block text-sm font-medium text-gray-700 mb-1'
const fieldErrorClass = 'mt-1 text-xs text-red-500'

type Field = 'name' | 'email' | 'password'
const FIELDS: Field[] = ['name', 'email', 'password']

interface ApiErrorBody {
  errors?: { detail: string; field?: string | null }[]
}

export default function RegisterPage() {
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<Field, string>>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const navigate = useNavigate()

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setFieldErrors({})
    setFormError(null)
    try {
      const res = await register(name, email, password)
      if (res.ok) {
        navigate('/login', { state: { registered: true } })
        return
      }
      const body: ApiErrorBody | null = await res.json().catch(() => null)
      const next: Partial<Record<Field, string>> = {}
      let general: string | null = null
      for (const error of body?.errors ?? []) {
        if (FIELDS.includes(error.field as Field)) next[error.field as Field] = error.detail
        else general ??= error.detail
      }
      setFieldErrors(next)
      if (general || Object.keys(next).length === 0)
        setFormError(general ?? 'Registration failed. Please try again.')
    } catch {
      setFormError('Network error. Is the server running?')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold text-gray-900">Create an account</h1>

      <form onSubmit={handleSubmit} className="rounded-xl border border-gray-200 bg-white p-8 flex flex-col gap-5">
        <div>
          <label htmlFor="register-name" className={labelClass}>Name</label>
          <input id="register-name" required minLength={2} maxLength={100}
            aria-invalid={!!fieldErrors.name} aria-errormessage="register-name-error"
            value={name} onChange={e => setName(e.target.value)} className={inputClass} />
          {fieldErrors.name && <p id="register-name-error" className={fieldErrorClass}>{fieldErrors.name}</p>}
        </div>

        <div>
          <label htmlFor="register-email" className={labelClass}>Email</label>
          <input id="register-email" type="email" required maxLength={200}
            aria-invalid={!!fieldErrors.email} aria-errormessage="register-email-error"
            value={email} onChange={e => setEmail(e.target.value)} className={inputClass} />
          {fieldErrors.email && <p id="register-email-error" className={fieldErrorClass}>{fieldErrors.email}</p>}
        </div>

        <div>
          <label htmlFor="register-password" className={labelClass}>Password</label>
          <input id="register-password" type="password" required minLength={8} aria-describedby="register-password-hint"
            aria-invalid={!!fieldErrors.password} aria-errormessage="register-password-error"
            value={password} onChange={e => setPassword(e.target.value)} className={inputClass} />
          <p id="register-password-hint" className="mt-1 text-xs text-gray-500">At least 8 characters.</p>
          {fieldErrors.password && <p id="register-password-error" className={fieldErrorClass}>{fieldErrors.password}</p>}
        </div>

        {formError && <p role="alert" className="text-sm text-red-500">{formError}</p>}

        <button
          type="submit"
          disabled={submitting}
          className="bg-gray-800 hover:bg-gray-900 text-white px-5 py-2 rounded-lg text-sm font-medium transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
        >
          {submitting ? 'Creating account…' : 'Create account'}
        </button>
      </form>
    </div>
  )
}
