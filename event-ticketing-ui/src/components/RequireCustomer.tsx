import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../context/useAuth'

/** Customer pages (my orders, calendar, profile): admins can't book, so they go to the admin area instead. */
export default function RequireCustomer() {
  const { user, loading } = useAuth()

  if (loading) return null

  if (user?.role === 'admin')
    return <Navigate to="/admin" replace />

  return <Outlet />
}
