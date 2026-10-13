import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { fetchMyPaymentMethod, type SavedCard } from '../api/paymentMethod'
import { useAuth } from '../context/useAuth'
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
        </section>
      )}
    </div>
  )
}
