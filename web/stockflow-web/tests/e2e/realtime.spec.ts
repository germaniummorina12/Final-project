import { expect, test } from '@playwright/test';
import { apiAs, apiUrl, createProduct } from './support';

test('a stock change made elsewhere shows up without a reload', async ({ browser }) => {
  const manager = await apiAs('manager');
  const product = await createProduct(manager, 5);

  // Window 1: a manager looking at the product list.
  const watcher = await browser.newContext({ storageState: 'e2e/.auth/manager.json' });
  const page = await watcher.newPage();
  await page.goto('/products');
  await page.getByRole('searchbox', { name: 'Search' }).fill(product.sku);
  const stockCell = page.getByRole('row', { name: new RegExp(product.sku) }).getByRole('cell').nth(4);
  await expect(stockCell).toHaveText(/^\s*5\s*$/);

  // "Another computer": a clerk sells two units through the API.
  const clerk = await apiAs('clerk');
  const customers = await (await clerk.get(`${apiUrl}/customers`)).json();
  const response = await clerk.post(`${apiUrl}/orders`, {
    data: { customerId: customers[0].id, lines: [{ productId: product.id, quantity: 2 }] },
  });
  expect(response.status()).toBe(201);

  // The first window changes by itself (SignalR): there is no page.reload() here.
  await expect(stockCell).toHaveText(/^\s*3\s*$/);

  await watcher.close();
});