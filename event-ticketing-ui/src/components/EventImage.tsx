import { useState } from 'react'

interface Props {
  src: string
  alt: string
  className?: string
}

/** An event's photo; if it can't be loaded (say the file was deleted), a neutral placeholder takes its place. */
export default function EventImage({ src, alt, className = '' }: Props) {
  // Remembers which URL failed, so a new image (an admin's edit arriving live) gets its own chance.
  const [failedSrc, setFailedSrc] = useState<string | null>(null)

  if (failedSrc === src) {
    return (
      <div
        role="img"
        aria-label={`${alt} (image unavailable)`}
        className={`bg-gray-100 text-gray-400 text-sm flex items-center justify-center ${className}`}
      >
        Image unavailable
      </div>
    )
  }
  return <img src={src} alt={alt} className={className} onError={() => setFailedSrc(src)} />
}
