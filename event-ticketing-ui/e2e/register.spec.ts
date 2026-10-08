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
