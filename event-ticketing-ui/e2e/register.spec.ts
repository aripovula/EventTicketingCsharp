import { expect, test } from '@playwright/test'

test('the login page links to the register page', async ({ page }) => {
  await page.goto('/login')

  await page.getByRole('link', { name: 'Create one' }).click()

  await expect(page).toHaveURL('/register')
  await expect(page.getByRole('heading', { name: 'Create an account' })).toBeVisible()
})

test('creating an account goes to the login page', async ({ page }) => {
  await page.route('**/api/v1/auth/register', route =>
    route.fulfill({ status: 201, json: { data: { userId: 9, name: 'New User', email: 'new.user@example.com', role: 'user' }, errors: [] } }),
  )
  await page.goto('/register')

  await page.getByLabel('Name').fill('New User')
  await page.getByLabel('Email').fill('new.user@example.com')
  await page.getByLabel('Password').fill('secret-password')
  await page.getByRole('button', { name: 'Create account' }).click()

  await expect(page).toHaveURL('/login')
})

test('a taken email is reported under the email field', async ({ page }) => {
  await page.route('**/api/v1/auth/register', route =>
    route.fulfill({
      status: 409,
      json: { data: null, errors: [{ code: 'email_taken', detail: 'An account with this email already exists.', field: 'email' }] },
    }),
  )
  await page.goto('/register')

  await page.getByLabel('Name').fill('Another John')
  await page.getByLabel('Email').fill('john@example.com')
  await page.getByLabel('Password').fill('secret-password')
  await page.getByRole('button', { name: 'Create account' }).click()

  await expect(page.getByText('An account with this email already exists.')).toBeVisible()
  await expect(page).toHaveURL('/register')
})
