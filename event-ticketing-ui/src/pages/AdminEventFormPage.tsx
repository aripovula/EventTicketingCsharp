import { Link } from 'react-router-dom'

export default function AdminEventFormPage() {
  return (
    <div>
      <Link to="/admin" className="text-sm text-cyan-600 hover:text-cyan-800 transition-colors no-underline">
        ← Back to admin
      </Link>

      <h1 className="text-2xl font-bold text-gray-900 mt-6 mb-6">New event</h1>
    </div>
  )
}
