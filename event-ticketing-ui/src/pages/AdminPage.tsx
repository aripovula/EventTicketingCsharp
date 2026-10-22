import { useState } from 'react'
import { Link } from 'react-router-dom'
import { deleteEvent } from '../api/events'
import ConfirmDialog from '../components/ConfirmDialog'
import { useEventPages } from '../hooks/useEventPages'
import TechNote from '../components/TechNote'

export default function AdminPage() {
  const { events, setEvents, loading, loadingMore, sentinelRef } = useEventPages({ sort: 'name' })
  const [confirmDeleteId, setConfirmDeleteId] = useState<number | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  async function handleDeleteConfirmed() {
    if (confirmDeleteId === null) return
    const res = await deleteEvent(confirmDeleteId).catch(() => null)
    if (!res?.ok) {
      setDeleteError(`Failed to delete event (${res?.status ?? 'network error'}). Please try again.`)
      setConfirmDeleteId(null)
      return
    }
    setEvents(prev => prev.filter(ev => ev.id !== confirmDeleteId))
    setConfirmDeleteId(null)
  }

  const confirmingEvent = events.find(ev => ev.id === confirmDeleteId)

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mt-0 mb-6">Admin</h1>
      <TechNote points={[
        <><strong>Role-based authorization:</strong> every admin endpoint requires the admin role in your JWT. Hiding buttons from other users is only for convenience; the API enforces it.</>,
        <><strong>Real-time push:</strong> each create, edit or delete is broadcast over SignalR, so every open page, customers&apos; included, updates at once.</>,
        <><strong>Audit trail:</strong> every change is written to an audit log in the same database transaction as the change itself.</>,
        <><strong>Cache eviction:</strong> changes evict the Redis-cached event list pages, so nobody sees stale data.</>,
      ]} />

      {deleteError && (
        <p role="alert" className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2 mb-4">{deleteError}</p>
      )}

      {loading ? (
        <p className="text-gray-500 text-center py-12">Loading...</p>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <ul aria-label="admin events" className="divide-y divide-gray-100 list-none p-0 m-0">
            {events.map(ev => (
              <li key={ev.id} className="flex items-center justify-between px-6 py-4 gap-4">
                <div>
                  <p className="font-medium text-gray-900">{ev.title}</p>
                  <p className="text-sm text-gray-500">{ev.venue}</p>
                </div>
                <div className="flex gap-2 shrink-0">
                  <Link
                    to={`/admin/${ev.id}/edit`}
                    aria-label={`Edit ${ev.title}`}
                    className="text-sm border border-gray-300 text-gray-600 px-3 py-1.5 rounded-lg hover:bg-gray-50 transition-colors no-underline"
                  >
                    Edit
                  </Link>
                  <button
                    onClick={() => setConfirmDeleteId(ev.id)}
                    aria-label={`Delete ${ev.title}`}
                    className="text-sm border border-red-200 text-red-600 px-3 py-1.5 rounded-lg hover:bg-red-50 transition-colors"
                  >
                    Delete
                  </button>
                </div>
              </li>
            ))}
          </ul>
          <div ref={sentinelRef} data-testid="admin-events-sentinel" className="h-1" />
          {loadingMore && <p className="text-gray-500 text-center py-6">Loading more events...</p>}
        </div>
      )}

      {confirmDeleteId !== null && (
        <ConfirmDialog
          title="Delete event?"
          message={<>Are you sure you want to delete <span className="font-medium text-gray-900">"{confirmingEvent?.title}"</span>? This cannot be undone.</>}
          confirmLabel="Delete"
          onConfirm={handleDeleteConfirmed}
          onCancel={() => setConfirmDeleteId(null)}
        />
      )}
    </div>
  )
}
