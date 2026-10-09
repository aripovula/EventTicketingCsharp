import { useState } from 'react'
import { Link } from 'react-router-dom'
import { EVENT_TYPES } from '../utils/eventTypes'

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-cyan-400 transition'
const labelClass = 'block text-sm font-medium text-gray-700 mb-1'

export interface EventFormData {
  title: string
  venue: string
  description: string
  imageUrl: string
  startTime: string
  endTime: string
  eventType: string
  totalSeats: string
  availableSeats: string
  price: string
}

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
  const [form, setForm] = useState<EventFormData>(emptyForm)

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target
    setForm(prev => ({ ...prev, [name]: value }))
  }

  return (
    <div>
      <Link to="/admin" className="text-sm text-cyan-600 hover:text-cyan-800 transition-colors no-underline">
        ← Back to admin
      </Link>

      <h1 className="text-2xl font-bold text-gray-900 mt-6 mb-6">New event</h1>

      <div className="bg-white rounded-xl border border-gray-200 p-6">
        <form aria-label="event form" onSubmit={e => e.preventDefault()} className="space-y-4">
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
