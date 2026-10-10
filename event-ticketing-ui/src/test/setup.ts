import '@testing-library/jest-dom/vitest'

// No test may reach the network: an unmocked request fails fast and visibly instead of
// hitting localhost and rejecting after the test has finished (which made runs flaky).
beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn((url: unknown) => Promise.reject(new Error(`Unmocked fetch in test: ${String(url)}`))))
})

// Likewise no test may open a real SignalR connection: pages get an inert hub client.
// Tests that check the hub wiring mock '@microsoft/signalr' (or useHubEvents) themselves.
vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: class {
    withUrl() { return this }
    withAutomaticReconnect() { return this }
    configureLogging() { return this }
    build() { return { on() {}, start: () => Promise.resolve(), stop: () => Promise.resolve() } }
  },
  LogLevel: { Warning: 3 },
}))
