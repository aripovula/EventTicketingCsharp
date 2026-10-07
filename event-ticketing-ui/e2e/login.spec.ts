import { expect, test } from '@playwright/test'

test('Sign in link opens the login page', async ({ page }) => {
  await page.goto('/')

  await page.getByRole('link', { name: 'Sign in' }).click()

  await expect(page).toHaveURL('/login')
  await expect(page.getByRole('heading', { name: 'Sign in', exact: true })).toBeVisible()
})

test('login page offers the demo-account panels', async ({ page }) => {
  await page.goto('/login')

  await expect(page.getByRole('heading', { name: 'Sign in as a regular user' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Sign in as admin' })).toBeVisible()
  await expect(page.getByRole('radio', { name: /john doe/i })).toBeChecked()
})

test('signing in as a demo user goes to the home page', async ({ page }) => {
  await page.route('**/api/v1/auth/login', route =>
    route.fulfill({ json: { data: { userId: 1, name: 'John Doe', email: 'john@example.com', role: 'user' }, errors: [] } }),
  )
  await page.goto('/login')

  await page.getByRole('button', { name: 'Sign in' }).first().click()

  await expect(page).toHaveURL('/')
})

test('a rejected sign-in shows the server error', async ({ page }) => {
  await page.route('**/api/v1/auth/login', route =>
    route.fulfill({
      status: 401,
      json: { data: null, errors: [{ code: 'invalid_credentials', detail: 'Invalid email or password.' }] },
    }),
  )
  await page.goto('/login')

  await page.getByRole('button', { name: 'Sign in' }).first().click()

  await expect(page.getByRole('alert')).toHaveText('Invalid email or password.')
  await expect(page).toHaveURL('/login')
})
