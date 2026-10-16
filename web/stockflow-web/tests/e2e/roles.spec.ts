import { expect, test } from '@playwright/test';
import { apiAs, apiUrl, uniqueSku } from './support';

test.describe('a manager', () => {
  test.use({ storageState: 'e2e/.auth/manager.json' });

  test('sees the manager actions on the product list', async ({ page }) => {
    await page.goto('/products');

    await expect(page.getByRole('heading', { name: 'Products' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'New product' })).toBeVisible();
  });
});

test.describe('a clerk', () => {
  test.use({ storageState: 'e2e/.auth/clerk.json' });

  test('does not see the manager actions', async ({ page }) => {
    await page.goto('/products');

    await expect(page.getByRole('heading', { name: 'Products' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'New product' })).toHaveCount(0);

    await page.goto('/orders');
    await expect(page.getByRole('heading', { name: 'Orders' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Cancel order' })).toHaveCount(0);
  });

  test('is sent back to the product list from /products/new', async ({ page }) => {
    await page.goto('/products/new');

    await expect(page).toHaveURL(/\/products$/);
  });

  test('is also refused by the API: the guard is not the security', async () => {
    const clerk = await apiAs('clerk');

    const response = await clerk.post(`${apiUrl}/products`, {
      data: { sku: uniqueSku(), name: 'Nope', categoryId: 1, unitPrice: 5, initialStock: 1, reorderLevel: 0 },
    });

    expect(response.status()).toBe(403);
  });
});