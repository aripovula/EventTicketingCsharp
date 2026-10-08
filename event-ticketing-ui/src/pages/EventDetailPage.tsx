import { Link } from 'react-router-dom'

export default function EventDetailPage() {
  return (
    <div>
      <Link to="/" className="text-sm text-cyan-600 hover:text-cyan-800 transition-colors no-underline">
        ← Back to events
      </Link>
    </div>
  )
}
