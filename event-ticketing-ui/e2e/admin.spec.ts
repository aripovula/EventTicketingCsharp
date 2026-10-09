import { expect, test, type Page } from '@playwright/test'

async function signedInAs(page: Page, role: 'admin' | 'user') {
  await page.route('**/api/v1/auth/me', route =>
    route.fulfill({ json: { data: { userId: 4, name: role === 'admin' ? 'Admin' : 'John Doe', email: `${role}@example.com`, role }, errors: [] } }),
  )
  await page.route('**/api/v1/events?**', route => route.fulfill({ json: { data: [], errors: [], meta: { nextCursor: null } } }))
}

test('an admin reaches the admin page from the header', async ({ page }) => {
  await signedInAs(page, 'admin')
  await page.goto('/')

  await page.getByRole('banner').getByRole('link', { name: 'Admin' }).click()

  await expect(page).toHaveURL('/admin')
  await expect(page.getByRole('heading', { name: 'Admin' })).toBeVisible()
})

test('a regular user is told they lack permission', async ({ page }) => {
  await signedInAs(page, 'user')
  await page.goto('/admin')

  await expect(page.getByText('You do not have permission to view this page.')).toBeVisible()
})
