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
