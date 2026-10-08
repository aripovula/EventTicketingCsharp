import { expect, test } from '@playwright/test'

test('the home page shows the events list', async ({ page }) => {
  await page.goto('/')

  await expect(page.getByRole('list', { name: 'events' })).toBeAttached()
})
