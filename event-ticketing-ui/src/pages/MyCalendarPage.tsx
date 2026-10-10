import { Navigate } from 'react-router-dom'
import { useAuth } from '../context/useAuth'

export default function MyCalendarPage() {
  const { user, loading } = useAuth()

  if (loading) return null
  if (!user) return <Navigate to="/login" replace />

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">My Calendar</h1>
    </div>
  )
}
