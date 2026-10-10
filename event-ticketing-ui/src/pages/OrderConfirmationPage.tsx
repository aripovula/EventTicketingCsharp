import { Link } from 'react-router-dom'

export default function OrderConfirmationPage() {
  return (
    <div className="max-w-lg mx-auto">
      <div className="mt-6 text-center">
        <Link to="/" className="text-sm text-cyan-600 hover:text-cyan-800 transition-colors no-underline">
          ← Browse more events
        </Link>
      </div>
    </div>
  )
}
