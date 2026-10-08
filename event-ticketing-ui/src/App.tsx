import { BrowserRouter, Link, Route, Routes, useNavigate } from 'react-router-dom'
import { useAuth } from './context/useAuth'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'

function HeaderNav() {
  const { user, signOut } = useAuth()
  const navigate = useNavigate()

  async function handleSignOut() {
    await signOut().catch(() => {})
    navigate('/login')
  }

  return (
    <nav className="flex items-center gap-4">
      {user ? (
        <button
          onClick={handleSignOut}
          className="text-sm border border-gray-300 text-gray-600 px-3 py-1 rounded-lg hover:bg-gray-50 transition-colors"
        >
          Sign out
        </button>
      ) : (
        <Link to="/login" className="text-sm text-gray-500 hover:text-cyan-600 transition-colors">
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
            <Route path="/" element={null} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  )
}

export default App
