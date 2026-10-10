import { act, renderHook } from '@testing-library/react'
import { useHubEvents } from './useHubEvents'

const connection = vi.hoisted(() => ({
  on: vi.fn(),
  start: vi.fn(() => Promise.resolve()),
  stop: vi.fn(() => Promise.resolve()),
}))
const withUrl = vi.hoisted(() => vi.fn())

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: class {
    withUrl(url: string) { withUrl(url); return this }
    withAutomaticReconnect() { return this }
    configureLogging() { return this }
    build() { return connection }
  },
  LogLevel: { Warning: 3 },
}))

function push(name: string, eventId: number) {
  const handler = connection.on.mock.calls.find(([n]) => n === name)?.[1] as (id: number) => void
  act(() => handler(eventId))
}

describe('useHubEvents', () => {
  beforeEach(() => vi.clearAllMocks())

  it('connects to the ticketing hub on mount and disconnects on unmount', () => {
    const { unmount } = renderHook(() => useHubEvents({ BookingMade: vi.fn() }))

    expect(withUrl).toHaveBeenCalledWith('/hubs/ticketing')
    expect(connection.start).toHaveBeenCalledTimes(1)
    unmount()
    expect(connection.stop).toHaveBeenCalledTimes(1)
  })

  it('calls only the handler for the pushed message, with the event id', () => {
    const onBooking = vi.fn()
    const onCreated = vi.fn()
    renderHook(() => useHubEvents({ BookingMade: onBooking, EventCreated: onCreated }))

    push('BookingMade', 42)

    expect(onBooking).toHaveBeenCalledWith(42)
    expect(onCreated).not.toHaveBeenCalled()
  })

  it('uses the latest handler without reconnecting', () => {
    const first = vi.fn()
    const latest = vi.fn()
    const { rerender } = renderHook(({ cb }) => useHubEvents({ EventUpdated: cb }), { initialProps: { cb: first } })

    rerender({ cb: latest })
    push('EventUpdated', 7)

    expect(connection.start).toHaveBeenCalledTimes(1)
    expect(latest).toHaveBeenCalledWith(7)
    expect(first).not.toHaveBeenCalled()
  })

  it('warns when the hub cannot be reached', async () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    connection.start.mockReturnValueOnce(Promise.reject(new Error('offline')))

    renderHook(() => useHubEvents({ BookingMade: vi.fn() }))
    await act(async () => {})

    expect(warn).toHaveBeenCalledWith('Live updates unavailable:', expect.any(Error))
    warn.mockRestore()
  })
})
