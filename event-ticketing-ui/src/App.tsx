import { BrowserRouter, Link, Route, Routes } from 'react-router-dom'
import LoginPage from './pages/LoginPage'

function App() {
  return (
    <BrowserRouter>
      <div className="min-h-screen bg-gray-50 text-gray-800">
        <header className="bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
          <Link to="/" className="text-xl font-semibold text-gray-900 hover:text-cyan-600 transition-colors">
            🎟 Event Ticketing
          </Link>
          <nav className="flex items-center gap-4">
            <Link to="/login" className="text-sm text-gray-500 hover:text-cyan-600 transition-colors">
              Sign in
            </Link>
          </nav>
        </header>
        <main className="max-w-4xl mx-auto px-4 py-8">
          <Routes>
            <Route path="/" element={null} />
            <Route path="/login" element={<LoginPage />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  )
}

export default App
