import { Fragment, type ReactNode } from 'react'

/**
 * A neutral card under a page's heading that says, in one short paragraph, what the page does
 * behind the scenes. The points are joined with semicolons and the paragraph ends with a full stop.
 */
export default function TechNote({ points, className = 'mb-6' }: { points: ReactNode[]; className?: string }) {
  return (
    <aside
      aria-label="How this page works"
      className={`rounded-xl border border-gray-200 bg-gray-50 px-5 py-4 text-sm text-gray-700 leading-relaxed ${className}`}
    >
      <p className="m-0">
        <strong className="text-gray-900">How this page works:</strong>{' '}
        {points.map((point, i) => (
          <Fragment key={i}>{point}{i < points.length - 1 ? '; ' : '.'}</Fragment>
        ))}
      </p>
    </aside>
  )
}
