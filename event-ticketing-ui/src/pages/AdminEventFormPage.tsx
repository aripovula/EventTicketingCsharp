import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { createEvent, fetchEvent, updateEvent } from '../api/events'
import { IMAGE_TYPES, uploadImage } from '../api/uploads'
import { eventToForm, toEventInput, type EventFormData } from '../utils/eventForm'
import { EVENT_TYPES } from '../utils/eventTypes'
import TechNote from '../components/TechNote'

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
  const [uploading, setUploading] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const navigate = useNavigate()

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target
    setForm(prev => ({ ...prev, [name]: value }))
  }

  async function handleImageChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    setUploading(true)
    setUploadError(null)
    try {
      const imageUrl = await uploadImage(file)
      setForm(prev => ({ ...prev, imageUrl }))
    } catch (err) {
      setUploadError(err instanceof Error ? err.message : 'The upload failed. Please try again.')
    } finally {
      setUploading(false)
      // Lets the admin pick the same file again after an error.
      e.target.value = ''
    }
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
    const input = toEventInput(form)
    const res = await (isEdit ? updateEvent(Number(id), input) : createEvent(input)).catch(() => null)
    if (res?.ok) {
      navigate('/admin')
      return
    }
    const body = await res?.json().catch(() => null)
    const details: string[] = (body?.errors ?? []).map((error: { detail: string }) => error.detail)
    setSubmitError(details.length > 0
      ? details.join(' ')
      : `Failed to ${isEdit ? 'update' : 'create'} event (${res?.status ?? 'network error'}). Please try again.`)
  }

  if (loading) return <p className="text-gray-500 text-center py-12">Loading...</p>
  if (loadError) return <p className="text-center text-red-500 py-12">{loadError}</p>

  return (
    <div>
      <Link to="/admin" className="text-sm text-cyan-600 hover:text-cyan-800 transition-colors no-underline">
        ← Back to admin
      </Link>

      <h1 className="text-2xl font-bold text-gray-900 mt-6 mb-6">{isEdit ? 'Edit event' : 'New event'}</h1>
      <TechNote points={[
        <><strong>pre-signed upload URL (Azure SAS)</strong>: after checking that you are an admin, the API gives the browser a URL that can write one new file for 5 minutes</>,
        <>the image goes <strong>straight to Azure Blob Storage</strong>, bypassing our server and saving its bandwidth and compute; in a social app, this is what lets uploads scale</>,
        <>the URL can&apos;t read, list or delete, and the server picks the file name, so nothing is overwritten (JPEG, PNG, WebP or GIF, up to 5 MB)</>,
      ]} />

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
            <label htmlFor="image" className={labelClass}>Image</label>
            <input
              id="image"
              type="file"
              accept={IMAGE_TYPES.join(',')}
              onChange={handleImageChange}
              disabled={uploading}
              className="block text-sm text-gray-600 file:mr-3 file:rounded-lg file:border file:border-gray-300 file:bg-white file:px-3 file:py-1.5 file:text-sm file:text-gray-700 hover:file:bg-gray-50"
            />
            <p className="text-xs text-gray-500 mt-1">JPEG, PNG, WebP or GIF, up to 5 MB.</p>
            {uploading && <p className="text-sm text-gray-500 mt-1">Uploading…</p>}
            {uploadError && <p role="alert" className="text-sm text-red-500 mt-1">{uploadError}</p>}
            {form.imageUrl && (
              <div className="mt-2 flex items-end gap-3">
                <img src={form.imageUrl} alt="Event image preview" className="h-32 w-auto rounded-lg object-cover border border-gray-200" />
                <button
                  type="button"
                  onClick={() => setForm(prev => ({ ...prev, imageUrl: '' }))}
                  className="text-sm text-gray-500 hover:text-red-600 transition-colors"
                >
                  Remove image
                </button>
              </div>
            )}
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
            <button type="submit" disabled={uploading} className="bg-cyan-600 text-white px-5 py-2 rounded-lg text-sm font-medium hover:bg-cyan-700 transition-colors disabled:opacity-40 disabled:cursor-not-allowed">
              {isEdit ? 'Save changes' : 'Create event'}
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
