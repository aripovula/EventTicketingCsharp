import { expect, test } from '@playwright/test'

test('the login page links to the register page', async ({ page }) => {
  await page.goto('/login')

  await page.getByRole('link', { name: 'Create one' }).click()

  await expect(page).toHaveURL('/register')
  await expect(page.getByRole('heading', { name: 'Create an account' })).toBeVisible()
})
