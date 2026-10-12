import { defineConfig, devices } from '@playwright/test'

// UI-focused E2E tests: the Vite dev server serves the app and tests mock the
// API with page.route(), so no backend or database is needed.
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  // One local retry absorbs load spikes on a busy dev machine; Playwright still reports such tests as flaky.
  retries: process.env.CI ? 2 : 1,
  // Each worker is a Chromium instance; on a small dev machine more than two starve each other of memory.
  workers: process.env.CI ? undefined : 2,
  reporter: 'list',
  // First visits make Vite compile pages on demand, which on a busy machine can exceed the 5s default.
  expect: { timeout: 15_000 },
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'npm run dev',
    // Overrides any key in .env.local, so the booking form offers Stripe's test cards.
    env: { VITE_STRIPE_PUBLISHABLE_KEY: '' },
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
  },
})
