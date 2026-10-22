import { BrowserRouter, Link, Route, Routes, useNavigate } from 'react-router-dom'
import RequireAdmin from './components/RequireAdmin'
import RequireCustomer from './components/RequireCustomer'
import { useAuth } from './context/useAuth'
import AdminCalendarPage from './pages/AdminCalendarPage'
import AdminEventFormPage from './pages/AdminEventFormPage'
import AdminOrdersPage from './pages/AdminOrdersPage'
import AdminPage from './pages/AdminPage'
import AdminSummaryPage from './pages/AdminSummaryPage'
import EventDetailPage from './pages/EventDetailPage'
import EventListPage from './pages/EventListPage'
import LoginPage from './pages/LoginPage'
import MyCalendarPage from './pages/MyCalendarPage'
import MyOrdersPage from './pages/MyOrdersPage'
import OrderConfirmationPage from './pages/OrderConfirmationPage'
import ProfilePage from './pages/ProfilePage'
import RegisterPage from './pages/RegisterPage'

function HeaderNav() {
  const { user, signOut } = useAuth()
  const navigate = useNavigate()

  async function handleSignOut() {
    await signOut().catch(() => {})
    navigate('/login')
  }

  const linkClass = 'text-sm text-gray-500 hover:text-cyan-600 transition-colors'

  return (
    <nav className="flex items-center gap-4">
      {user?.role === 'admin' ? (
        <>
          <Link to="/admin" className={linkClass}>Events</Link>
          <Link to="/admin/calendar" className={linkClass}>Events Calendar</Link>
          <Link to="/admin/summary" className={linkClass}>Seat summary</Link>
          <Link to="/admin/orders" className={linkClass}>All orders</Link>
          <Link to="/admin/new" className={linkClass}>+ New event</Link>
        </>
      ) : (
        <>
          <Link to="/orders" className={linkClass}>My orders</Link>
          {user && <Link to="/calendar" className={linkClass}>My Calendar</Link>}
          {user && <Link to="/profile" className={linkClass}>Profile</Link>}
        </>
      )}
      {user ? (
        <button
          onClick={handleSignOut}
          className="text-sm border border-gray-300 text-gray-600 px-3 py-1 rounded-lg hover:bg-gray-50 transition-colors"
        >
          Sign out
        </button>
      ) : (
        <Link to="/login" className={linkClass}>
          Sign in
        </Link>
      )}
    </nav>
  )
}

function App() {
  return (
    <BrowserRouter>
      <div className="min-h-screen bg-gray-50 text-gray-800">
        <header className="bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
          <Link to="/" className="text-xl font-semibold text-gray-900 hover:text-cyan-600 transition-colors">
            🎟 Event Ticketing
          </Link>
          <HeaderNav />
        </header>
        <main className="max-w-4xl mx-auto px-4 py-8">
          <Routes>
            <Route path="/" element={<EventListPage />} />
            <Route path="/events/:id" element={<EventDetailPage />} />
            <Route path="/orders/:id" element={<OrderConfirmationPage />} />
            <Route element={<RequireCustomer />}>
              <Route path="/orders" element={<MyOrdersPage />} />
              <Route path="/calendar" element={<MyCalendarPage />} />
              <Route path="/profile" element={<ProfilePage />} />
            </Route>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route element={<RequireAdmin />}>
              <Route path="/admin" element={<AdminPage />} />
              <Route path="/admin/calendar" element={<AdminCalendarPage />} />
              <Route path="/admin/orders" element={<AdminOrdersPage />} />
              <Route path="/admin/summary" element={<AdminSummaryPage />} />
              <Route path="/admin/new" element={<AdminEventFormPage />} />
              <Route path="/admin/:id/edit" element={<AdminEventFormPage />} />
            </Route>
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  )
}

export default App
