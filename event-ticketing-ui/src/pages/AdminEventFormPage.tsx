import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { createEvent, fetchEvent } from '../api/events'
import { eventToForm, toEventInput, type EventFormData } from '../utils/eventForm'
import { EVENT_TYPES } from '../utils/eventTypes'

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-cyan-400 transition'
const labelClass = 'block text-sm font-medium text-gray-700 mb-1'

const emptyForm: EventFormData = {
  title: '',
  venue: '',
  description: '',
  imageUrl: '',
  startTime: '',
  endTime: '',
  eventType: 'Music',
  totalSeats: '',
  availableSeats: '',
  price: '',
}

export default function AdminEventFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = id !== undefined
  const [form, setForm] = useState<EventFormData>(emptyForm)
  const [loading, setLoading] = useState(isEdit)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const navigate = useNavigate()

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target
    setForm(prev => ({ ...prev, [name]: value }))
  }

  useEffect(() => {
    if (!isEdit) return
    fetchEvent(Number(id))
      .then(async res => {
        if (!res.ok) throw new Error(`Failed to load event (${res.status}).`)
        const body = await res.json()
        setForm(eventToForm(body.data))
      })
      .catch((err: Error) => setLoadError(err.message.startsWith('Failed to load') ? err.message : 'Could not load event.'))
      .finally(() => setLoading(false))
  }, [id, isEdit])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setSubmitError(null)
    const res = await createEvent(toEventInput(form)).catch(() => null)
    if (res?.ok) {
      navigate('/admin')
      return
    }
    const body = await res?.json().catch(() => null)
    const details: string[] = (body?.errors ?? []).map((error: { detail: string }) => error.detail)
    setSubmitError(details.length > 0
      ? details.join(' ')
      : `Failed to create event (${res?.status ?? 'network error'}). Please try again.`)
  }

  if (loading) return <p className="text-gray-500 text-center py-12">Loading...</p>
  if (loadError) return <p className="text-center text-red-500 py-12">{loadError}</p>

  return (
    <div>
      <Link to="/admin" className="text-sm text-cyan-600 hover:text-cyan-800 transition-colors no-underline">
        ← Back to admin
      </Link>

      <h1 className="text-2xl font-bold text-gray-900 mt-6 mb-6">{isEdit ? 'Edit event' : 'New event'}</h1>

      <div className="bg-white rounded-xl border border-gray-200 p-6">
        <form aria-label="event form" onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div>
              <label htmlFor="title" className={labelClass}>Title</label>
              <input id="title" name="title" value={form.title} onChange={handleChange} required maxLength={200} className={inputClass} />
            </div>
            <div>
              <label htmlFor="venue" className={labelClass}>Venue</label>
              <input id="venue" name="venue" value={form.venue} onChange={handleChange} required maxLength={200} className={inputClass} />
            </div>
          </div>

          <div>
            <label htmlFor="description" className={labelClass}>Description</label>
            <textarea id="description" name="description" value={form.description} onChange={handleChange} required maxLength={2000} rows={3} className={inputClass} />
          </div>

          <div>
            <label htmlFor="imageUrl" className={labelClass}>Image URL</label>
            <input id="imageUrl" name="imageUrl" type="url" value={form.imageUrl} onChange={handleChange} maxLength={500} className={inputClass} />
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <div>
              <label htmlFor="startTime" className={labelClass}>Start time</label>
              <input id="startTime" name="startTime" type="datetime-local" value={form.startTime} onChange={handleChange} required className={inputClass} />
            </div>
            <div>
              <label htmlFor="endTime" className={labelClass}>End time</label>
              <input id="endTime" name="endTime" type="datetime-local" value={form.endTime} onChange={handleChange} required className={inputClass} />
            </div>
            <div>
              <label htmlFor="eventType" className={labelClass}>Event type</label>
              <select id="eventType" name="eventType" value={form.eventType} onChange={handleChange} className={inputClass}>
                {EVENT_TYPES.map(type => <option key={type} value={type}>{type}</option>)}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <div>
              <label htmlFor="totalSeats" className={labelClass}>Total seats</label>
              <input id="totalSeats" name="totalSeats" type="number" min="1" max="10000" value={form.totalSeats} onChange={handleChange} required className={inputClass} />
            </div>
            <div>
              <label htmlFor="availableSeats" className={labelClass}>Available seats</label>
              <input id="availableSeats" name="availableSeats" type="number" min="0" max="10000" value={form.availableSeats} onChange={handleChange} required className={inputClass} />
            </div>
            <div>
              <label htmlFor="price" className={labelClass}>Price ($)</label>
              <input id="price" name="price" type="number" min="0" step="0.01" value={form.price} onChange={handleChange} required className={inputClass} />
            </div>
          </div>

          {submitError && (
            <p role="alert" className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2">{submitError}</p>
          )}

          <div className="flex gap-3 pt-2">
            <button type="submit" className="bg-cyan-600 text-white px-5 py-2 rounded-lg text-sm font-medium hover:bg-cyan-700 transition-colors">
              Create event
            </button>
            <Link to="/admin" className="border border-gray-300 text-gray-600 px-5 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 transition-colors no-underline">
              Cancel
            </Link>
          </div>
        </form>
      </div>
    </div>
  )
}
