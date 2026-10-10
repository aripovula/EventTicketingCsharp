import { Link } from 'react-router-dom'
import { useAuth } from '../context/useAuth'

export default function MyOrdersPage() {
  const { user, loading } = useAuth()

  if (loading) return <p className="text-gray-500 text-center py-12">Loading...</p>

  return (
    <div className="max-w-lg mx-auto">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">My orders</h1>
      {!user && (
        <p className="text-sm text-gray-600">
          <Link to="/login" className="text-cyan-600 hover:underline">Sign in</Link> to see your orders.
          Booked as a guest? Use the link on your order confirmation.
        </p>
      )}
    </div>
  )
}
