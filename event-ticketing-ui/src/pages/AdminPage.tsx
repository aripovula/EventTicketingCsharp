import { Link } from 'react-router-dom'

export default function AdminPage() {
  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900 m-0">Admin</h1>
        <div className="flex gap-3">
          <Link
            to="/admin/new"
            className="bg-cyan-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-cyan-700 transition-colors no-underline"
          >
            + New event
          </Link>
        </div>
      </div>
    </div>
  )
}
