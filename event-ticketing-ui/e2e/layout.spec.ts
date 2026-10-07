import { expect, test } from '@playwright/test'

test('shows the app title in the header', async ({ page }) => {
  await page.goto('/')

  await expect(page.getByRole('banner')).toContainText('Event Ticketing')
})
