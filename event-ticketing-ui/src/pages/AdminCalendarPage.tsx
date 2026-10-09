import { Link } from 'react-router-dom'
import { EVENT_TYPE_COLORS, EVENT_TYPES } from '../utils/eventTypes'

export default function AdminCalendarPage() {
  return (
    <div>
      <div className="mb-6">
        <Link to="/admin" className="text-sm text-cyan-600 hover:text-cyan-800 transition-colors no-underline">
          ← Back to admin
        </Link>
        <h1 className="text-2xl font-bold text-gray-900 m-0">Events Calendar</h1>
      </div>

      <ul aria-label="event type legend" className="flex flex-wrap gap-3 mb-5 list-none p-0 m-0">
        {EVENT_TYPES.filter(t => t !== 'Other').map(type => (
          <li key={type} className="flex items-center gap-1.5 text-xs font-medium text-gray-600">
            <span className="w-3 h-3 rounded-full inline-block" style={{ backgroundColor: EVENT_TYPE_COLORS[type] }} />
            {type}
          </li>
        ))}
      </ul>
      <p className="text-xs text-gray-400 mb-4">
        Each event shows: <em>Title – remaining seats</em>. Overlapping events share the same time slot.
      </p>
    </div>
  )
}
