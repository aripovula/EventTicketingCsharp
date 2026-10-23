import { useState } from 'react'
import { Link } from 'react-router-dom'
import { archiveEvent, deleteEvent, fetchEvent } from '../api/events'
import ConfirmDialog from '../components/ConfirmDialog'
import { useEventPages } from '../hooks/useEventPages'
import { useHubEvents } from '../hooks/useHubEvents'
import TechNote from '../components/TechNote'

export default function AdminPage() {
  const { events, setEvents, reload, loading, loadingMore, sentinelRef } = useEventPages({ sort: 'name' })
  // Events with orders can't be deleted (the orders would go too), so they get Archive instead of Delete.
  const [confirming, setConfirming] = useState<{ id: number; action: 'delete' | 'archive' } | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  function refreshRow(eventId: number) {
    if (!events.some(e => e.id === eventId)) return
    fetchEvent(eventId)
      .then(res => (res.ok ? res.json() : null))
      .then(body => { if (body?.data) setEvents(prev => prev.map(e => (e.id === eventId ? body.data : e))) })
      .catch(() => {})
  }
  const removeRow = (eventId: number) => setEvents(prev => prev.filter(e => e.id !== eventId))
  // A sold ticket turns Delete into Archive at once, without waiting for a reload.
  useHubEvents({
    BookingMade: eventId => setEvents(prev => prev.map(e => (e.id === eventId ? { ...e, hasOrders: true } : e))),
    EventCreated: reload,
    EventUpdated: refreshRow,
    EventDeleted: removeRow,
    EventArchived: removeRow,
  })

  async function handleConfirmed() {
    if (confirming === null) return
    const { id, action } = confirming
    setConfirming(null)
    const res = await (action === 'delete' ? deleteEvent(id) : archiveEvent(id)).catch(() => null)
    if (res?.status === 409 && action === 'delete') {
      // Someone bought a ticket since the list loaded.
      setEvents(prev => prev.map(ev => (ev.id === id ? { ...ev, hasOrders: true } : ev)))
      setActionError('Tickets have been sold for this event, so it can\'t be deleted. Archive it instead.')
      return
    }
    if (!res?.ok) {
      setActionError(`Failed to ${action} event (${res?.status ?? 'network error'}). Please try again.`)
      return
    }
    setActionError(null)
    setEvents(prev => prev.filter(ev => ev.id !== id))
  }

  const confirmingEvent = events.find(ev => ev.id === confirming?.id)

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mt-0 mb-6">Admin</h1>
      <TechNote points={[
        <><strong>role-based authorization</strong>: every admin endpoint checks the admin role in your JWT; hidden buttons are only for convenience</>,
        <>each change is pushed to every open page over <strong>SignalR</strong>, written to an <strong>audit log</strong> in the same transaction, and evicts the <strong>Redis</strong>-cached lists</>,
        <>an event with sold tickets is <strong>archived</strong> instead of deleted: it leaves the list and stops selling, but its orders stay</>,
      ]} />

      {actionError && (
        <p role="alert" className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2 mb-4">{actionError}</p>
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
                  {ev.hasOrders ? (
                    <button
                      onClick={() => setConfirming({ id: ev.id, action: 'archive' })}
                      aria-label={`Archive ${ev.title}`}
                      className="text-sm border border-amber-300 text-amber-700 px-3 py-1.5 rounded-lg hover:bg-amber-50 transition-colors"
                    >
                      Archive
                    </button>
                  ) : (
                    <button
                      onClick={() => setConfirming({ id: ev.id, action: 'delete' })}
                      aria-label={`Delete ${ev.title}`}
                      className="text-sm border border-red-200 text-red-600 px-3 py-1.5 rounded-lg hover:bg-red-50 transition-colors"
                    >
                      Delete
                    </button>
                  )}
                </div>
              </li>
            ))}
          </ul>
          <div ref={sentinelRef} data-testid="admin-events-sentinel" className="h-1" />
          {loadingMore && <p className="text-gray-500 text-center py-6">Loading more events...</p>}
        </div>
      )}

      {confirming?.action === 'delete' && (
        <ConfirmDialog
          title="Delete event?"
          message={<>Are you sure you want to delete <span className="font-medium text-gray-900">"{confirmingEvent?.title}"</span>? This cannot be undone.</>}
          confirmLabel="Delete"
          onConfirm={handleConfirmed}
          onCancel={() => setConfirming(null)}
        />
      )}
      {confirming?.action === 'archive' && (
        <ConfirmDialog
          title="Archive event?"
          message={<>Tickets have been sold for <span className="font-medium text-gray-900">"{confirmingEvent?.title}"</span>, so it can't be deleted. Archiving takes it off sale and out of the events list; its orders stay.</>}
          confirmLabel="Archive"
          onConfirm={handleConfirmed}
          onCancel={() => setConfirming(null)}
        />
      )}
    </div>
  )
}
