import { useEffect, useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { fetchMyPaymentMethod, saveMyPaymentMethod, type SavedCard } from '../api/paymentMethod'
import { useAuth } from '../context/useAuth'
import CardInput, { type CreatePaymentMethod } from '../payments/CardInput'
import { formatCard, formatCardExpiry } from '../utils/formatCard'

export default function ProfilePage() {
  const { user, loading: authLoading } = useAuth()
  const [card, setCard] = useState<SavedCard | null>(null)
  const [cardLoading, setCardLoading] = useState(true)

  useEffect(() => {
    if (!user) return
    fetchMyPaymentMethod()
      .then(res => (res.ok ? res.json() : null))
      .then(body => setCard(body?.data ?? null))
      .catch(() => {})
      .finally(() => setCardLoading(false))
  }, [user])

  if (authLoading) return <p className="text-gray-500 text-center py-12">Loading...</p>

  return (
    <div className="max-w-lg mx-auto space-y-8">
      <h1 className="text-2xl font-bold text-gray-900">Profile</h1>
      {!user && (
        <p className="text-sm text-gray-600">
          <Link to="/login" className="text-cyan-600 hover:underline">Sign in</Link> to see your profile.
        </p>
      )}

      {user && (
        <section className="bg-white rounded-xl border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Account</h2>
          <dl className="space-y-3 text-sm">
            <div className="flex justify-between">
              <dt className="text-gray-500">Name</dt>
              <dd className="font-medium text-gray-900">{user.name}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-gray-500">Email</dt>
              <dd className="font-medium text-gray-900">{user.email}</dd>
            </div>
          </dl>
        </section>
      )}

      {user && (
        <section aria-labelledby="saved-card-heading" className="bg-white rounded-xl border border-gray-200 p-6">
          <h2 id="saved-card-heading" className="text-lg font-semibold text-gray-900 mb-4">Saved card</h2>
          {!cardLoading && card && (
            <div className="mb-5 p-4 bg-gray-50 rounded-lg border border-gray-200 text-sm">
              <p className="font-medium text-gray-900">{formatCard(card)}</p>
              <p className="text-gray-500 mt-1">Expires {formatCardExpiry(card)}</p>
            </div>
          )}
          {!cardLoading && !card && (
            <p className="text-sm text-gray-500 mb-5">No saved card yet.</p>
          )}
          <CardInput>
            {input => (
              <SaveCardForm
                ready={input.ready}
                createPaymentMethod={input.createPaymentMethod}
                field={input.field}
                note={input.note}
                replacing={!!card}
                onSaved={setCard}
              />
            )}
          </CardInput>
        </section>
      )}
    </div>
  )
}

type SaveCardFormProps = {
  ready: boolean
  createPaymentMethod: CreatePaymentMethod
  field: ReactNode
  note: ReactNode
  replacing: boolean
  onSaved: (card: SavedCard) => void
}

function SaveCardForm({ ready, createPaymentMethod, field, note, replacing, onSaved }: SaveCardFormProps) {
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSaved(false)
    setSaving(true)
    try {
      const card = await createPaymentMethod()
      if ('error' in card) {
        setError(card.error)
        return
      }
      const res = await saveMyPaymentMethod(card.id).catch(() => null)
      const body = await res?.json().catch(() => null)
      if (res?.ok) {
        onSaved(body.data)
        setSaved(true)
      } else if (res?.status === 402) {
        setError(body?.errors?.[0]?.detail ?? 'Your card was declined.')
      } else {
        setError('Saving the card failed. Please try again.')
      }
    } finally {
      setSaving(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} aria-label="save card form" className="space-y-4">
      {field}
      {error && <p role="alert" className="text-sm text-red-500">{error}</p>}
      {saved && <p role="status" className="text-sm text-green-600">Card saved.</p>}
      <button
        type="submit"
        disabled={saving || !ready}
        className="bg-cyan-600 text-white px-5 py-2 rounded-lg text-sm font-medium hover:bg-cyan-700 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
      >
        {saving ? 'Saving…' : replacing ? 'Replace card' : 'Save card'}
      </button>
      <p className="text-xs text-gray-500 leading-relaxed">{note}</p>
    </form>
  )
}
