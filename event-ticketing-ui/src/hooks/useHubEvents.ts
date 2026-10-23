import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useEffect, useLayoutEffect, useRef } from 'react'

/** Messages the TicketingHub pushes; each carries the id of the event that changed. */
export type HubEvent = 'BookingMade' | 'EventCreated' | 'EventUpdated' | 'EventDeleted' | 'EventArchived'

type Handlers = Partial<Record<HubEvent, (eventId: number) => void>>

/**
 * Connects to the TicketingHub while the component is mounted and calls the matching handler for
 * each pushed message. Reconnects automatically after network blips. The set of event names is read
 * once on mount; the handler functions themselves may change on every render without reconnecting.
 */
export function useHubEvents(handlers: Handlers): void {
  const handlersRef = useRef(handlers)
  useLayoutEffect(() => {
    handlersRef.current = handlers
  })
  const eventNamesRef = useRef(Object.keys(handlers) as HubEvent[])

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/ticketing')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()
    for (const name of eventNamesRef.current) {
      connection.on(name, (eventId: number) => handlersRef.current[name]?.(eventId))
    }

    let stopped = false
    connection.start().catch(err => {
      // Unmounting mid-negotiation (e.g. StrictMode's double mount) rejects start(); that's expected.
      if (!stopped) console.warn('Live updates unavailable:', err)
    })
    return () => {
      stopped = true
      void connection.stop()
    }
  }, [])
}
