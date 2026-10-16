import { expect, test } from '@playwright/test';
import { apiAs, apiUrl, createProduct } from './support';

test.use({ storageState: 'e2e/.auth/manager.json' });

test('placing an order reduces the stock', async ({ page }) => {
  const api = await apiAs('manager');
  const product = await createProduct(api, 5);

  await page.goto('/orders/new');
  await page.getByLabel('Customer').selectOption({ label: 'Acme Corp' });
  await page
    .getByLabel('Product for line 1')
    .selectOption({ label: `${product.sku}: ${product.name} (5 in stock)` });
  await page.getByLabel('Quantity for line 1').fill('2');
  await expect(page.getByText(/^Total:/)).toContainText('20.00');

  await page.getByRole('button', { name: 'Place order' }).click();
  await expect(page).toHaveURL(/\/orders$/);

  await page.goto('/products');
  await page.getByRole('searchbox', { name: 'Search' }).fill(product.sku);
  const stockCell = page.getByRole('row', { name: new RegExp(product.sku) }).getByRole('cell').nth(4);
  await expect(stockCell).toHaveText(/^\s*3\s*$/);
});

test('ordering more than the stock shows the message and changes nothing', async ({ page }) => {
  const api = await apiAs('manager');
  const product = await createProduct(api, 2);

  await page.goto('/orders/new');
  await page.getByLabel('Customer').selectOption({ label: 'Acme Corp' });
  await page
    .getByLabel('Product for line 1')
    .selectOption({ label: `${product.sku}: ${product.name} (2 in stock)` });
  await page.getByLabel('Quantity for line 1').fill('5');
  await page.getByRole('button', { name: 'Place order' }).click();

  await expect(page.getByRole('alert')).toContainText('Insufficient stock');
  await expect(page).toHaveURL(/\/orders\/new$/);

  const after = await (await api.get(`${apiUrl}/products/${product.id}`)).json();
  expect(after.stockOnHand).toBe(2);
});