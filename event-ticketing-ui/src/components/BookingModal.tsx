import { useState } from 'react'
import { formatPrice } from '../utils/formatPrice'

type Props = {
  eventTitle: string
  eventDate: string
  eventVenue: string
  priceCents: number
  savedEmail?: string
  onConfirm: (email: string) => Promise<void>
  onClose: () => void
  error: string | null
}

const labelClass = 'block text-sm font-medium text-gray-700 mb-1'

export default function BookingModal({ eventTitle, eventDate, eventVenue, priceCents, savedEmail, onConfirm, onClose, error }: Props) {
  const [email, setEmail] = useState(savedEmail ?? '')
  const [submitting, setSubmitting] = useState(false)

  const formattedDate = new Date(eventDate).toLocaleDateString(undefined, { dateStyle: 'long' })

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    try {
      await onConfirm(email)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div role="dialog" aria-modal="true" aria-labelledby="booking-modal-title" className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-xl border border-gray-200 p-6 w-full max-w-md mx-4 shadow-lg">
        <h2 id="booking-modal-title" className="text-xl font-bold text-gray-900 mb-1">Place order</h2>
        <p className="text-sm text-gray-500 mb-4">
          {eventTitle} · {eventVenue} · {formattedDate} &mdash; <span className="font-medium text-gray-700">{formatPrice(priceCents)}</span>
        </p>

        <form onSubmit={handleSubmit} aria-label="booking form" className="space-y-4">
          <div>
            <label htmlFor="booking-email" className={labelClass}>Email</label>
            <input
              id="booking-email"
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
              placeholder="you@example.com"
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-cyan-400 transition"
            />
          </div>

          {error && <p role="alert" className="text-sm text-red-500">{error}</p>}

          <div className="flex gap-3 pt-2">
            <button
              type="submit"
              disabled={submitting}
              className="bg-cyan-600 text-white px-5 py-2 rounded-lg text-sm font-medium hover:bg-cyan-700 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
            >
              {submitting ? 'Placing order…' : 'Place order'}
            </button>
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="border border-gray-300 text-gray-600 px-5 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 transition-colors disabled:opacity-40"
            >
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
