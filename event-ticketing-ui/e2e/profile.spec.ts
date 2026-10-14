import { expect, test, type Page } from '@playwright/test'

const visa = { brand: 'visa', last4: '4242', expMonth: 12, expYear: 2029 }

async function signedIn(page: Page) {
  await page.route('**/api/v1/auth/me', route =>
    route.fulfill({ json: { data: { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }, errors: [] } }))
}

test('the profile page shows the account and the saved card', async ({ page }) => {
  await signedIn(page)
  await page.route('**/api/v1/auth/me/payment-method', route => route.fulfill({ json: { data: visa, errors: [] } }))
  await page.goto('/profile')

  await expect(page.getByText('john@example.com')).toBeVisible()
  await expect(page.getByText('Visa ending in 4242')).toBeVisible()
  await expect(page.getByText('Expires 12/2029')).toBeVisible()
})

test('a signed-in user opens their profile from the header', async ({ page }) => {
  await signedIn(page)
  await page.route('**/api/v1/auth/me/payment-method', route =>
    route.fulfill({ status: 404, json: { data: null, errors: [{ code: 'not_found', detail: 'No saved card.' }] } }))
  await page.goto('/')

  await page.getByRole('banner').getByRole('link', { name: 'Profile' }).click()

  await expect(page).toHaveURL('/profile')
  await expect(page.getByText('No saved card yet.')).toBeVisible()
})

test('a signed-in user saves a card on the profile page', async ({ page }) => {
  await signedIn(page)
  let saved: unknown = null
  await page.route('**/api/v1/auth/me/payment-method', route => {
    if (route.request().method() === 'PUT') {
      saved = visa
      return route.fulfill({ json: { data: saved, errors: [] } })
    }
    return saved
      ? route.fulfill({ json: { data: saved, errors: [] } })
      : route.fulfill({ status: 404, json: { data: null, errors: [{ code: 'not_found', detail: 'No saved card.' }] } })
  })
  await page.goto('/profile')

  await expect(page.getByText('No saved card yet.')).toBeVisible()
  // No Stripe key in E2E runs, so the form offers Stripe's test cards.
  await page.getByRole('button', { name: 'Save card' }).click()

  await expect(page.getByRole('status')).toHaveText('Card saved.')
  await expect(page.getByText('Visa ending in 4242')).toBeVisible()
  await page.reload()
  await expect(page.getByText('Visa ending in 4242')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Replace card' })).toBeVisible()
})

test('saving a card while payments are unavailable asks the user to wait', async ({ page }) => {
  await signedIn(page)
  await page.route('**/api/v1/auth/me/payment-method', route =>
    route.request().method() === 'PUT'
      ? route.fulfill({ status: 503, headers: { 'Retry-After': '30' }, json: { data: null, errors: [{ code: 'payments_unavailable' }] } })
      : route.fulfill({ status: 404, json: { data: null, errors: [{ code: 'not_found', detail: 'No saved card.' }] } }))
  await page.goto('/profile')

  await page.getByRole('button', { name: 'Save card' }).click()

  await expect(page.getByRole('alert')).toHaveText('Payments are temporarily unavailable. Please try again in 30 seconds.')
})
