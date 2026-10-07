import { expect, test } from '@playwright/test'

test('Sign in link opens the login page', async ({ page }) => {
  await page.goto('/')

  await page.getByRole('link', { name: 'Sign in' }).click()

  await expect(page).toHaveURL('/login')
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible()
})
