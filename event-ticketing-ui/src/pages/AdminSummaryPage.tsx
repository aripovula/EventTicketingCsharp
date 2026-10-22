import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { fetchSummary, type EventSummary } from '../api/admin'
import { useHubEvents } from '../hooks/useHubEvents'
import { formatPrice } from '../utils/formatPrice'
import TechNote from '../components/TechNote'

export default function AdminSummaryPage() {
  const [rows, setRows] = useState<EventSummary[]>([])
  const [asOf, setAsOf] = useState<Date | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)

  const loadSummary = useCallback(() => {
    fetchSummary()
      .then(res => { if (!res.ok) throw new Error(); return res.json() })
      .then(body => { setRows(body.data ?? []); setAsOf(new Date()) })
      .catch(() => setError(true))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => { loadSummary() }, [loadSummary])
  // Every booking changes the seat counts and revenue, so recount live.
  useHubEvents({ BookingMade: loadSummary })

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900 m-0">Seat summary</h1>
        <Link to="/admin" className="text-sm text-cyan-600 hover:text-cyan-800 transition-colors no-underline">
          ← Back to admin
        </Link>
      </div>
      <TechNote points={[
        <><strong>One SQL query:</strong> PostgreSQL works out the sold seats, remaining seats and revenue for every event in a single round trip.</>,
        <><strong>Live:</strong> the table reloads over SignalR whenever anyone books a ticket.</>,
      ]} />

      {loading && <p className="text-gray-500 text-center py-12">Loading...</p>}
      {error && <p className="text-red-500 text-center py-12">Failed to load summary.</p>}

      {!loading && !error && (
        <>
          <p className="text-sm text-gray-500 mb-4">
            Data as of{' '}
            <span className="font-medium text-gray-700">
              {asOf?.toLocaleString(undefined, { dateStyle: 'long', timeStyle: 'medium' })}
            </span>
          </p>
          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="text-left px-6 py-3 font-medium text-gray-500">Event</th>
                  <th className="text-right px-6 py-3 font-medium text-gray-500">Opening balance</th>
                  <th className="text-right px-6 py-3 font-medium text-gray-500">Sold</th>
                  <th className="text-right px-6 py-3 font-medium text-gray-500">Remaining</th>
                  <th className="text-right px-6 py-3 font-medium text-gray-500">Revenue</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {rows.map(row => (
                  <tr key={row.eventId} className="hover:bg-gray-50 transition-colors">
                    <td className="px-6 py-4 font-medium text-gray-900">{row.title}</td>
                    <td className="px-6 py-4 text-right text-gray-700">{row.totalSeats}</td>
                    <td className="px-6 py-4 text-right text-cyan-600 font-medium">{row.soldSeats}</td>
                    <td className="px-6 py-4 text-right text-gray-700">{row.remainingSeats}</td>
                    <td className="px-6 py-4 text-right font-medium text-green-600">{formatPrice(row.revenueCents)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  )
}
