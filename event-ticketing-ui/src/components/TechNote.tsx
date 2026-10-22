import type { ReactNode } from 'react'

/** A neutral card under a page's heading that tells visitors what the page does behind the scenes. */
export default function TechNote({ points, className = 'mb-6' }: { points: ReactNode[]; className?: string }) {
  return (
    <aside
      aria-label="How this page works"
      className={`rounded-xl border border-gray-200 bg-gray-50 px-5 py-4 text-sm text-gray-700 leading-relaxed ${className}`}
    >
      <p className="font-semibold text-gray-900 m-0 mb-2">How this page works</p>
      <ul className="list-disc pl-5 space-y-1 m-0">
        {points.map((point, i) => <li key={i}>{point}</li>)}
      </ul>
    </aside>
  )
}
