import '@testing-library/jest-dom/vitest'

// No test may reach the network: an unmocked request fails fast and visibly instead of
// hitting localhost and rejecting after the test has finished (which made runs flaky).
beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn((url: unknown) => Promise.reject(new Error(`Unmocked fetch in test: ${String(url)}`))))
})
