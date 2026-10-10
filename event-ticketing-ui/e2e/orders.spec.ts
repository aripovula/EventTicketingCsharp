import { expect, test } from '@playwright/test'

test('My orders asks a signed-out visitor to sign in', async ({ page }) => {
  await page.goto('/')

  await page.getByRole('banner').getByRole('link', { name: 'My orders' }).click()

  await expect(page).toHaveURL('/orders')
  await expect(page.getByRole('heading', { name: 'My orders' })).toBeVisible()
  await expect(page.getByRole('main').getByRole('link', { name: 'Sign in' })).toBeVisible()
})
